using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class SSAOFeature : ScriptableRendererFeature
{

    [System.Serializable]
    public class SSAOSettings {

        public RenderPassEvent renderPassEvent = RenderPassEvent.AfterRenderingOpaques;

        public Shader blitShader;
        public Texture2D noiseTexture;

        public float intensity = 1;
        public float noiseIntensity = 1;
        public float radius = .1f;
        public float area = 1;

        [Range(4, 64)]
        public int samples = 16;
    }

    class SSAOPostProcessPass : ScriptableRenderPass
    {
        Material m_Material;
        private RenderTargetHandle m_TemporaryColorTexture;
        private RenderTargetIdentifier m_Destination;
        private string m_ProfilerTag = "SSAO";

        private SSAOSettings settings;

        public SSAOPostProcessPass(SSAOSettings settings) {

            this.settings = settings;

            m_Material = new Material(settings.blitShader);
            m_TemporaryColorTexture.Init("_TemporaryColorTexture");
        }

        public void Setup (RenderTargetIdentifier colorBuffer) {
            m_Destination = colorBuffer;
        }

        // This method is called before executing the render pass.
        // It can be used to configure render targets and their clear state. Also to create temporary render target textures.
        // When empty this render pass will render to the active camera render target.
        // You should never call CommandBuffer.SetRenderTarget. Instead call <c>ConfigureTarget</c> and <c>ConfigureClear</c>.
        // The render pipeline will ensure target setup and clearing happens in an performance manner.

        public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor) {

            RenderTextureDescriptor opaqueDesc = cameraTextureDescriptor;
            opaqueDesc.depthBufferBits = 0;

            // Can't read and write to same color target, create a temp render target to blit.
            cmd.GetTemporaryRT(m_TemporaryColorTexture.id, opaqueDesc, FilterMode.Bilinear);

            if(m_Material == null) {
                m_Material = new Material(settings.blitShader);
            }

            // Ambient Occlusion Settings
            m_Material.SetTexture("_NoiseTex", settings.noiseTexture);
            m_Material.SetFloat("_AO_Intensity", settings.intensity);
            m_Material.SetFloat("_AO_Noise_Intensity", settings.noiseIntensity);
            m_Material.SetFloat("_AO_Radius", settings.radius);

            // SSAO settings
            m_Material.SetInt("_SSAO_Samples", settings.samples);
            m_Material.SetFloat("_SSAO_Area", settings.area);

        }
       

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData) {

            CommandBuffer cmd = CommandBufferPool.Get(m_ProfilerTag);

            Blit(cmd, m_Destination, m_TemporaryColorTexture.Identifier());
            Blit(cmd, m_TemporaryColorTexture.Identifier(), m_Destination, m_Material);

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        public override void FrameCleanup(CommandBuffer cmd) {
            cmd.ReleaseTemporaryRT(m_TemporaryColorTexture.id);
        }
    }

    [SerializeField] private SSAOSettings settings = new SSAOSettings();

    SSAOPostProcessPass m_ScriptablePass;

    public override void Create()
    {

        m_ScriptablePass = new SSAOPostProcessPass(settings);

        // Configures where the render pass should be injected.

        m_ScriptablePass.renderPassEvent = settings.renderPassEvent;
    }

    // Here you can inject one or multiple render passes in the renderer.
    // This method is called when setting up the renderer once per-camera.

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        m_ScriptablePass.Setup(renderer.cameraColorTarget);
        renderer.EnqueuePass(m_ScriptablePass);
    }
}


