using UnityEngine;
using UnityEngine.Rendering;

[ExecuteAlways]
[RequireComponent(typeof(Camera))]
public class Blur : MonoBehaviour
{
    [SerializeField] private Shader _blurShader;
    [SerializeField, Range(3, 16)] private int _maxIterations = 2;
    [SerializeField, Min(1)] private int _startDownscale = 2;
    [SerializeField, Min(1)] private int _downscaleLimit = 2;
    
    private Camera _camera;
    private Material _blurMat;
    
    private CommandBuffer _commandBuffer;

    private CommandBuffer RenderCommandBuffer
    {
        get
        {
            _commandBuffer ??= new CommandBuffer { name = "Render Blur" };
            return _commandBuffer;
        }
    }

    private const int MaxBlurPyramidLevels = 16;
    
    private static readonly int BlurSourceId = Shader.PropertyToID("_BlurSource");
    private static readonly int BlurSourceId2 = Shader.PropertyToID("_BlurSource2");
    private static readonly int BlurFinalId = Shader.PropertyToID("_BlurFinal");

    private const string BlurPyramidPropertyName = "_BlurPyramid";
    private static readonly int BlurPyramidId;

    static Blur()
    {
        BlurPyramidId = Shader.PropertyToID(BlurPyramidPropertyName + 0);
        for (var i = 1; i < MaxBlurPyramidLevels * 2; i++)
        {
            Shader.PropertyToID(BlurPyramidPropertyName + i);
        }
    }

    private void OnEnable()
    {
        if (!_blurShader)
            _blurShader = Shader.Find("Hidden/Blur");

        _blurMat = new Material(_blurShader);
        
        _camera = GetComponent<Camera>();
        _camera.AddCommandBuffer(CameraEvent.AfterHaloAndLensFlares, RenderCommandBuffer);
    }

    private void OnDisable()
    {
        _camera.RemoveCommandBuffer(CameraEvent.AfterHaloAndLensFlares, RenderCommandBuffer);

        DestroyBlurMaterial();
    }

    private void DestroyBlurMaterial()
    {
        #if UNITY_EDITOR
                if (!Application.isPlaying)
                {
                    DestroyImmediate(_blurMat);
                    return;
                }
        #endif
                Destroy(_blurMat);
    }

    private void OnDestroy()
    {
        _commandBuffer?.Dispose();
    }

    private void Draw(RenderTargetIdentifier from, RenderTargetIdentifier to, BlurPass pass)
    {
        RenderCommandBuffer.SetGlobalTexture(BlurSourceId, from);
        RenderCommandBuffer.SetRenderTarget(to, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
        RenderCommandBuffer.DrawProcedural(Matrix4x4.identity, _blurMat, (int)pass, MeshTopology.Triangles, 3);
    }

    private void Update()
    {
        RenderCommandBuffer.Clear();

        var width = _camera.pixelWidth / _startDownscale;
        var height = _camera.pixelHeight / _startDownscale;
        var initialWidth = width;
        var initialHeight = height;
        var format = RenderTextureFormat.Default;
        var fromId = BlurPyramidId;
        var toId = BlurPyramidId + 2;
        
        // copy camera to temporal buffer
        RenderCommandBuffer.GetTemporaryRT(BlurPyramidId, width, height, 0, FilterMode.Bilinear, format);
        Draw(BuiltinRenderTextureType.CameraTarget, BlurPyramidId, BlurPass.Copy);
        width /= 2;
        height /= 2;

        // downscale
        int i;
        for (i = 1; i < _maxIterations; i++)
        {
            if (height < _downscaleLimit || width < _downscaleLimit)
                break;
            
            RenderCommandBuffer.GetTemporaryRT(toId, width, height, 0, FilterMode.Bilinear, format);
            RenderCommandBuffer.GetTemporaryRT(toId - 1, width, height, 0, FilterMode.Bilinear, format);
            Draw(fromId, toId, BlurPass.BlurDualFilterDownSample);

            fromId = toId;
            toId += 2;
            width /= 2;
            height /= 2;
        }
        
        // upscale
        toId = fromId - 1;

        for (var j = i + 1; j > 0; j -= 2)
        {
            RenderCommandBuffer.SetGlobalTexture(BlurSourceId2, toId + 1);
            Draw(fromId, toId, BlurPass.BlurDualFilterUpSample);
            
            fromId = toId;
            toId -= 2;
        }
        
        RenderCommandBuffer.SetGlobalTexture(BlurFinalId, fromId);

        for (i -= 1; i >= 0; i--)
        {
            RenderCommandBuffer.ReleaseTemporaryRT(fromId);
            RenderCommandBuffer.ReleaseTemporaryRT(fromId - 1);
            fromId -= 2;
        }
    }
    
    private enum BlurPass : byte
    {
        Copy,
        BlurDualFilterDownSample,
        BlurDualFilterUpSample
    }
}
