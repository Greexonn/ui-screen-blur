using System;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.UI;

[ExecuteAlways]
[RequireComponent(typeof(Camera))]
public class Blur : MonoBehaviour
{
    [SerializeField, Min(1f)] private float _blurStrength = 1f;
    [SerializeField] private RawImage _targetElement;

    private float BufferScale => 1f / _blurStrength;
    
    private Camera _camera;
    private RenderTexture _scaledRenderTexture;

    private CommandBuffer _commandBuffer;

    private CommandBuffer CommandBuffer
    {
        get
        {
            _commandBuffer ??= new CommandBuffer { name = "Blur" };
            return _commandBuffer;
        }
    }

    private static readonly int GrabTextureId = Shader.PropertyToID("_GrabTexture");

    private void OnEnable()
    {
        _camera = GetComponent<Camera>();
        _camera.AddCommandBuffer(CameraEvent.AfterHaloAndLensFlares, CommandBuffer);

        CreateScaledRenderTexture();
    }

    private void OnDisable()
    {
        _camera.RemoveCommandBuffer(CameraEvent.AfterHaloAndLensFlares, CommandBuffer);
        
        RenderTexture.ReleaseTemporary(_scaledRenderTexture);
    }

    private void OnDestroy()
    {
        _commandBuffer?.Dispose();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (!_scaledRenderTexture)
            return;

        var newSize = GetScaledBufferSize();
        
        if (_scaledRenderTexture.width == newSize.x && _scaledRenderTexture.height == newSize.y)
            return;
        
        RenderTexture.ReleaseTemporary(_scaledRenderTexture);
        CreateScaledRenderTexture();
    }
#endif

    private void CreateScaledRenderTexture()
    {
        _scaledRenderTexture = RenderTexture.GetTemporary(GetTextureDescriptor(GetScaledBufferSize()));
        _scaledRenderTexture.name = "Scaled Blur RT";
    }

    private void OnPreRender()
    {
        CommandBuffer.Clear();
        
        CommandBuffer.ReleaseTemporaryRT(GrabTextureId);
        
        if (_targetElement == null || _targetElement.material == null)
            return;
        
        CommandBuffer.GetTemporaryRT(GrabTextureId, GetTextureDescriptor(GetBufferSize()));
        
        CommandBuffer.Blit(_camera.targetTexture, GrabTextureId);
        CommandBuffer.Blit(GrabTextureId, _scaledRenderTexture);
    }

    private void Update()
    {
        if (_targetElement)
            _targetElement.texture = _scaledRenderTexture;
    }

    private Vector2Int GetBufferSize()
    {
        return new Vector2Int
        {
            x = Mathf.Max(_camera.pixelWidth, 1),
            y = Mathf.Max(_camera.pixelHeight, 1)
        };
    }

    private Vector2Int GetScaledBufferSize()
    {
        var bufferScale = BufferScale;
        return new Vector2Int
        {
            x = Mathf.Max((int)(_camera.pixelWidth * bufferScale), 1),
            y = Mathf.Max((int)(_camera.pixelHeight * bufferScale), 1)
        };
    }

    private static RenderTextureDescriptor GetTextureDescriptor(Vector2Int bufferSize)
    {
        return new RenderTextureDescriptor(bufferSize.x, bufferSize.y)
        {
            graphicsFormat = GraphicsFormatUtility.GetGraphicsFormat(RenderTextureFormat.Default, RenderTextureReadWrite.Default),
            depthBufferBits = 0,
            msaaSamples = 1
        };
    }
}
