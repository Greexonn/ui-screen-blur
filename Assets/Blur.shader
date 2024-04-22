Shader "Hidden/Blur"
{
    SubShader
    {
    	ZTest Always
	    ZWrite Off
	    Cull Off
    	
    	HLSLINCLUDE
    	#include "DualFilter.hlsl"
    	ENDHLSL

		Pass
		{
			Name "Copy"
			
			HLSLPROGRAM
				#pragma target 3.5
				#pragma vertex DefaultPassVertex
				#pragma fragment CopyPassFragment
			ENDHLSL
		}
    	
        Pass
        {
        	Name "Blur DualFilter DownSample"
			
			HLSLPROGRAM
				#pragma target 3.0
				#pragma vertex DefaultPassVertex
				#pragma fragment BlurDualFilterDownSamplePassFragment
			ENDHLSL
        }

		Pass 
		{
			Name "Blur DualFilter UpSample"

			HLSLPROGRAM
				#pragma target 3.0
				#pragma vertex DefaultPassVertex
				#pragma fragment BlurDualFilterUpSamplePassFragment
			ENDHLSL
		}
    }
}
