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
    private RenderTexture _renderTexture;
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

    private void Awake()
    {
        _camera = GetComponent<Camera>();
    }

    private void OnDestroy()
    {
        _commandBuffer?.Dispose();
    }

    private void OnPreRender()
    {
        RenderTexture.ReleaseTemporary(_scaledRenderTexture);
        CommandBuffer.ReleaseTemporaryRT(GrabTextureId);
        
        ExecuteCommandBuffer();
    }

    private void ExecuteCommandBuffer()
    {
        Graphics.ExecuteCommandBuffer(_commandBuffer);
        _commandBuffer.Clear();
    }

    private void OnPostRender()
    {
        if (_targetElement == null || _targetElement.material == null)
            return;

        _scaledRenderTexture = RenderTexture.GetTemporary(GetTextureDescriptor(GetScaledBufferSize()));
        CommandBuffer.GetTemporaryRT(GrabTextureId, GetTextureDescriptor(GetBufferSize()));
        
        CommandBuffer.Blit(_camera.targetTexture, GrabTextureId);
        CommandBuffer.Blit(GrabTextureId, _scaledRenderTexture);
        
        ExecuteCommandBuffer();
        
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
