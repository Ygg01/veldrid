﻿using System;
using System.IO;
using System.Text;
using Veldrid.SPIRV;
using Veldrid.StbImage;

namespace Veldrid.SampleGallery
{
    public class SkyboxRenderer : DisposeCollectorBase
    {
        private Pipeline _pipeline;
        private readonly ResourceSet _set;

        public SkyboxRenderer(GraphicsDevice gd, Stream[] imageStreams)
            : base(gd)
        {
            (Shader[] shaders, SpirvReflection reflection) = ShaderUtil.LoadEmbeddedShaderSet(
                typeof(SkyboxRenderer).Assembly, Factory, "Skybox");
            ShaderSetDescription shaderSet = new ShaderSetDescription(
                Array.Empty<VertexLayoutDescription>(),
                shaders);

            ResourceLayout layout = Factory.CreateResourceLayout(new ResourceLayoutDescription(
                new ResourceLayoutElementDescription("SkyTex", ResourceKind.TextureReadOnly, ShaderStages.Fragment),
                new ResourceLayoutElementDescription("SkySamp", ResourceKind.Sampler, ShaderStages.Fragment)));

            GraphicsPipelineDescription gpd = new GraphicsPipelineDescription(
                BlendStateDescription.SingleOverrideBlend,
                gd.IsDepthRangeZeroToOne
                    ? DepthStencilStateDescription.DepthOnlyGreaterEqual
                    : DepthStencilStateDescription.DepthOnlyLessEqual,
                RasterizerStateDescription.Default,
                PrimitiveTopology.TriangleStrip,
                shaderSet,
                new[] { GalleryConfig.Global.CameraInfoLayout, layout },
                GalleryConfig.Global.MainFBOutput);

            gpd.ReflectedVertexElements = reflection.VertexElements;
            gpd.ReflectedResourceLayouts = reflection.ResourceLayouts;

            _pipeline = Factory.CreateGraphicsPipeline(gpd);

            Texture cubeTex = StbTextureLoader.LoadCube(gd, Factory, imageStreams, false, true);
            _set = Factory.CreateResourceSet(new ResourceSetDescription(
                layout,
                cubeTex,
                Device.Features.SamplerAnisotropy ? Device.Aniso4xSampler : Device.LinearSampler));
        }

        public void Render(CommandBuffer cb, uint frameIndex)
        {
            float depth = Device.IsDepthRangeZeroToOne ? 0 : 1;
            cb.SetViewport(0, new Viewport(0, 0, GalleryConfig.Global.ViewWidth, GalleryConfig.Global.ViewHeight, depth, depth));

            cb.BindPipeline(_pipeline);
            cb.BindGraphicsResourceSet(0, GalleryConfig.Global.CameraInfoSets[frameIndex]);
            cb.BindGraphicsResourceSet(1, _set);
            cb.Draw(4);
        }
    }
}