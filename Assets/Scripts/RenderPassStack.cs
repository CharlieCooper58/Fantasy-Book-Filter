using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.RenderGraphModule;
using static TMPro.SpriteAssetUtilities.TexturePacker_JsonArray;
using UnityEngine.Rendering.RenderGraphModule.Util;
using NUnit.Framework.Constraints;
using static Unity.VisualScripting.Member;
using Unity.Collections.LowLevel.Unsafe;
using System.ComponentModel;

public class RenderPassStack : ScriptableRendererFeature
{
    class RenderPassEventsStack : ScriptableRenderPass
    {
        [SerializeField] RenderTexture rt;
        private PassInfoSO[] passes;
        static readonly int _TFM_Id = Shader.PropertyToID("_TFM");

        Material linearClampBlitMaterial;
        public void Setup(PassInfoSO[] passes, Material linearClampMaterial)
        {
            this.passes = passes;
            linearClampBlitMaterial = linearClampMaterial;

        }
        // This class stores the data needed by the RenderGraph pass.
        // It is passed as a parameter to the delegate function that executes the RenderGraph pass.


  

        // RecordRenderGraph is where the RenderGraph handle can be accessed, through which render passes can be added to the graph.
        // FrameData is a context container through which URP resources can be accessed and managed.
        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            var resourceData = frameData.Get<UniversalResourceData>();
            var source = resourceData.activeColorTexture;
            var destinationDesc = renderGraph.GetTextureDesc(source);
            destinationDesc.clearBuffer = true;

            // Do an initial blit to copy the render texture
            // We do this so that we don't accidentally try to blit activeColorTexture onto itself if passes is empty
            TextureHandle currentSource = renderGraph.CreateTexture(destinationDesc);
            RenderGraphUtils.BlitMaterialParameters para = new(source, currentSource, linearClampBlitMaterial, 0);
            renderGraph.AddBlitPass(para, passName: "Copy source");

            for (int i = 0; i < passes.Length; i++)
            {
                switch(passes[i].m_passType) 
                {
                    case PassInfoSO.PassType.kuwahara:
                        currentSource = DoKuwaharaPass(renderGraph, currentSource, passes[i] as KuwaharaSettingsSO);
                        break;
                    case PassInfoSO.PassType.sharpness:
                        currentSource = DoSharpnessPass(renderGraph, currentSource, passes[i] as SharpnessSettingsSO);
                        break;
                    case PassInfoSO.PassType.downscale:
                        currentSource = DoDownscalePass(renderGraph, currentSource, passes[i] as  DownscaleSettingsSO);
                        break;
                    case PassInfoSO.PassType.colorClamp:
                        currentSource = DoColorClampPass(renderGraph, currentSource, passes[i] as ColorClampSettingsSO);
                        break;
                }
                
            }
            TextureHandle endColor = resourceData.activeColorTexture;
            para = new(currentSource, endColor, linearClampBlitMaterial, 0);
            renderGraph.AddBlitPass(para, passName: "Blit to active render texture");

        }

        #region Kuwahara Filter
        private TextureHandle DoKuwaharaPass(RenderGraph renderGraph, TextureHandle inputTex, KuwaharaSettingsSO settings)
        {
            var kuwaharaMat = settings.passMaterial;
            kuwaharaMat.SetInt("_KernelSize", settings.kernelSize);
            kuwaharaMat.SetInt("_N", 8);
            kuwaharaMat.SetFloat("_Q", settings.sharpness);
            kuwaharaMat.SetFloat("_Hardness", settings.hardness);
            kuwaharaMat.SetFloat("_Alpha", settings.alpha);
            kuwaharaMat.SetFloat("_ZeroCrossing", settings.zeroCrossing);
            kuwaharaMat.SetFloat("_Zeta", settings.useZeta ? settings.zeta : 2.0f / 2.0f / (settings.kernelSize / 2.0f));
            var destinationDesc = renderGraph.GetTextureDesc(inputTex);

            TextureHandle structuredTensor = renderGraph.CreateTexture(destinationDesc);
            RenderGraphUtils.BlitMaterialParameters para = new(inputTex, structuredTensor, kuwaharaMat, 0);
            renderGraph.AddBlitPass(para, passName: "Structured Tensor Pass");

            TextureHandle eigenvectors1 = renderGraph.CreateTexture(destinationDesc);
            para = new(structuredTensor, eigenvectors1, kuwaharaMat, 1);
            renderGraph.AddBlitPass(para, passName: "Eigenvectors Pass 1");

            TextureHandle eigenvectors2 = renderGraph.CreateTexture(destinationDesc);
            para = new(eigenvectors1, eigenvectors2, kuwaharaMat, 2);
            renderGraph.AddBlitPass(para, passName: "Eigenvectors Pass 2");

            TextureHandle[] kuwaharaPasses = new TextureHandle[settings.passes + 1];
            kuwaharaPasses[0] = inputTex;

            for (int i = 1; i < settings.passes+1; i++)
            {
                kuwaharaPasses[i] = renderGraph.CreateTexture(destinationDesc);
                AddCustomBlitPass(renderGraph, kuwaharaPasses[i - 1], kuwaharaPasses[i], "Kuwahara Filter " + i.ToString(), 3, kuwaharaMat, eigenvectors2);
            }
            return kuwaharaPasses[settings.passes];
        }
        private class KuwaharaPassData
        {
            public TextureHandle src;
            public TextureHandle dst;
            public TextureHandle tfm;

            public int passIndex;
            public Material mat;
        }
        // This static method is passed as the RenderFunc delegate to the RenderGraph render pass.
        // It is used to execute draw commands.
        static void ExecuteKuwaharaPass(KuwaharaPassData data, RasterGraphContext context)
        {
            data.mat.SetTexture("_TFM", data.tfm);
            Blitter.BlitTexture(context.cmd, data.src, new Vector4(1, 1, 0, 0), data.mat, data.passIndex);
        }
        void AddCustomBlitPass(RenderGraph renderGraph, TextureHandle source, TextureHandle destination, string passName, int passIndex, Material mat, TextureHandle additionalTexture)
        {
            using var builder = renderGraph.AddRasterRenderPass(passName, out KuwaharaPassData passData);

            builder.UseTexture(source, AccessFlags.Read);
            builder.UseTexture(additionalTexture, AccessFlags.Read);
            passData.src = source;
            passData.passIndex = passIndex;
            passData.tfm = additionalTexture;
            passData.mat = mat;
            builder.SetRenderAttachment(destination, 0);
            builder.SetRenderFunc<KuwaharaPassData>(ExecuteKuwaharaPass);
        }

        #endregion
        #region Sharpness Filter
        private TextureHandle DoSharpnessPass(RenderGraph renderGraph, TextureHandle inputTex, SharpnessSettingsSO settings)
        {
            var destinationDesc = renderGraph.GetTextureDesc(inputTex);
            settings.passMaterial.SetFloat("_Amount", settings.sharpnessAmount);
            TextureHandle sharpenedTexture = renderGraph.CreateTexture(destinationDesc);
            RenderGraphUtils.BlitMaterialParameters para = new(inputTex, sharpenedTexture, settings.passMaterial, 0);
            renderGraph.AddBlitPass(para, passName: "Sharpness Pass");
            return sharpenedTexture;
        }
        #endregion
        #region Color Clamp Filter
        private TextureHandle DoColorClampPass(RenderGraph renderGraph, TextureHandle inputTex, ColorClampSettingsSO settings)
        {
            settings.passMaterial.SetInt("_HueClamp", settings.hueClamp);
            settings.passMaterial.SetInt("_SatClamp", settings.satClamp);
            settings.passMaterial.SetInt("_ValueClamp", settings.valueClamp);
            
            settings.passMaterial.SetInt("_RClamp", settings.rClamp);
            settings.passMaterial.SetInt("_BClamp", settings.bClamp);
            settings.passMaterial.SetInt("_GClamp", settings.gClamp);
            settings.passMaterial.SetFloat("_DitherSpread", settings.ditherSpread);
            settings.passMaterial.SetFloat("_HueShift", settings.hueShift);
            if (settings.useHSV)
            {
                settings.passMaterial.EnableKeyword("USE_HSV");
            }
            else
            {
                settings.passMaterial.DisableKeyword("USE_HSV");
            }
            if (settings.dither)
            {
                settings.passMaterial.EnableKeyword("DO_DITHER");
            }
            else
            {
                settings.passMaterial.DisableKeyword("DO_DITHER");
            }
            if (settings.preserveLowerValues)
            {
                settings.passMaterial.EnableKeyword("PRESERVE_LOWER_VALUES");
            }
            else
            {
                settings.passMaterial.DisableKeyword("PRESERVE_LOWER_VALUES");
            }
            if (settings.useSameClampForAllChannels)
            {
                settings.passMaterial.EnableKeyword("USE_SAME_CLAMP_FOR_ALL_CHANNELS");
            }
            else
            {
                settings.passMaterial.DisableKeyword("USE_SAME_CLAMP_FOR_ALL_CHANNELS");
            }
            var destinationDesc = renderGraph.GetTextureDesc(inputTex);
            TextureHandle colorClamp = renderGraph.CreateTexture(destinationDesc);
            RenderGraphUtils.BlitMaterialParameters para = new(inputTex, colorClamp, settings.passMaterial, 0);
            renderGraph.AddBlitPass(para, passName: "Color Clamp");
            return colorClamp;
        }
        #endregion
        #region Downscale Pass
        private TextureHandle DoDownscalePass(RenderGraph renderGraph, TextureHandle inputTex, DownscaleSettingsSO settings)
        {
            var destinationDesc = renderGraph.GetTextureDesc(inputTex);
            TextureHandle currentSource = inputTex;
            float width = destinationDesc.width;
            float height = destinationDesc.height;
            for (int i = 0; i < settings.numDownscales; i++)
            {
                width /= settings.downscaleAmount;
                height /= settings.downscaleAmount;
                TextureDesc downscaledDesc = destinationDesc;
                downscaledDesc.width = (int)width;
                downscaledDesc.height = (int)height;
                TextureHandle downscaledTexture = renderGraph.CreateTexture(downscaledDesc);
                RenderGraphUtils.BlitMaterialParameters downscaleParameters = new(currentSource, downscaledTexture, settings.passMaterial, 0);
                renderGraph.AddBlitPass(downscaleParameters, passName: "Downscale " + i.ToString());
                currentSource = downscaledTexture;
            }
            return currentSource;
        }
        #endregion
    }
    


    //[SerializeField] public float ditherSpread;

    public RenderPassEvent renderPassEvent = RenderPassEvent.AfterRenderingTransparents;
    public Material linearClampBlitMat;

    public PassInfoSO[] RenderPasses;
    RenderPassEventsStack m_ScriptablePass;

    /// <inheritdoc/>
    public override void Create()
    {
        m_ScriptablePass = new RenderPassEventsStack();
        //m_ScriptablePass.Setup(settings);

        // Configures where the render pass should be injected.
        m_ScriptablePass.renderPassEvent = renderPassEvent;
    }

    // Here you can inject one or multiple render passes in the renderer.
    // This method is called when setting up the renderer once per-camera.
    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        m_ScriptablePass.Setup(RenderPasses, linearClampBlitMat);

        renderer.EnqueuePass(m_ScriptablePass);
    }
}





