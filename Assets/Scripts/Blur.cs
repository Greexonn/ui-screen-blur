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

    private CommandBuffer _commandBuffer;

    private CommandBuffer CommandBuffer
    {
        get
        {
            _commandBuffer ??= new CommandBuffer { name = "Blur" };
            return _commandBuffer;
        }
    }

    private static readonly int BlurSourceId = Shader.PropertyToID("_BlurSource");

    private void OnEnable()
    {
        _camera = GetComponent<Camera>();
        _camera.AddCommandBuffer(CameraEvent.AfterHaloAndLensFlares, CommandBuffer);
    }

    private void OnDisable()
    {
        _camera.RemoveCommandBuffer(CameraEvent.AfterHaloAndLensFlares, CommandBuffer);
    }

    private void OnDestroy()
    {
        _commandBuffer?.Dispose();
    }

    private void Update()
    {
        CommandBuffer.Clear();
        
        CommandBuffer.ReleaseTemporaryRT(BlurSourceId);
        
        if (_targetElement == null || _targetElement.material == null)
            return;

        var scaledSize = GetScaledBufferSize();
        CommandBuffer.GetTemporaryRT(BlurSourceId, scaledSize.x, scaledSize.y, 0, FilterMode.Bilinear, RenderTextureFormat.Default);
        
        CommandBuffer.Blit(BuiltinRenderTextureType.CameraTarget, BlurSourceId);
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
}
