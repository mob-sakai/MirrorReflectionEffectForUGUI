using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using Coffee.UIExtensions;

namespace Coffee.UIExtensions
{
	public class UIReflect : BaseMeshEffect
	{
		//################################
		// Serialize Members.
		//################################
		[SerializeField][Range (0, 1)] float m_Alpha = 0.5f;
		[SerializeField][Range (1, 500)] float m_Height = 50f;
		[SerializeField] float m_Space = 50f;
		[SerializeField] Color m_StartColor = new Color (1, 1, 1, 0.75f);
		[SerializeField] Color m_EndColor = new Color (1, 1, 1, 0);

		float height {
			get{ return m_Height; }
		}

		//################################
		// Public Members.
		//################################
		public override void ModifyMesh (VertexHelper vh)
		{
			// Invalid.
			if (!isActiveAndEnabled || vh.currentVertCount == 0 || (vh.currentVertCount % 4 != 0 && vh.currentVertCount % 6 != 0)) {
				return;
			}

			_rect = graphic.rectTransform.rect;

			var quad = UIVertexUtil.s_QuadVerts;
			var inputVerts = UIVertexUtil.s_InputVerts;
			var outputVerts = UIVertexUtil.s_OutputVerts;

			inputVerts.Clear ();
			outputVerts.Clear ();

			vh.GetUIVertexStream (inputVerts);

			for (int i = 0; i < inputVerts.Count; i += 6) {
				if (graphic is Text) {
					quad [0] = inputVerts [i + 4];	// bottom-left
					quad [1] = inputVerts [i + 0];	// top-left
					quad [2] = inputVerts [i + 1];	// top-right
					quad [3] = inputVerts [i + 2];	// bottom-right
				} else {
					quad [0] = inputVerts [i + 0];	// bottom-left
					quad [1] = inputVerts [i + 1];	// top-left
					quad [2] = inputVerts [i + 2];	// top-right
					quad [3] = inputVerts [i + 4];	// bottom-right
				}
				UIVertexUtil.AddQuadToStream (quad, outputVerts);	// origin quad
				AddReflectedQuad (quad, outputVerts);	// reflected quad
			}

			vh.Clear ();
			vh.AddUIVertexTriangleStream (outputVerts);

			inputVerts.Clear ();
			outputVerts.Clear ();
		}


		//################################
		// Private Members.
		//################################
		Rect _rect;

		void AddReflectedQuad (UIVertex[] quad, List<UIVertex> result)
		{
			// Read the existing quad vertices
			UIVertex v0 = quad [0];	// bottom-left
			UIVertex v1 = quad [1];	// top-left
			UIVertex v2 = quad [2];	// top-right
			UIVertex v3 = quad [3];	// bottom-right

			// Reflection is unnecessary.
			if (height < (v0.position.y - _rect.yMin) && height < (v3.position.y - _rect.yMin)) {
//			Debug.LogFormat ("リフレクト不要 {0}, {1}", (v0.position.y - _rect.yMin), (v1.position.y - _rect.yMin));
//			|| v0.position.y < m_Height && v3.position.y < m_Height ) {
				return;
			}

			// Trim quad.
			if (height < (v1.position.y - _rect.yMin) || height < (v2.position.y - _rect.yMin)) {
//			Debug.LogFormat ("分割必要 {0}, {1}", (v1.position.y - _rect.yMin), height);
				v1 = UIVertexUtil.Lerp (v0, v1, GetLerpFactor (v0.position.y, v1.position.y));
				v2 = UIVertexUtil.Lerp (v3, v2, GetLerpFactor (v3.position.y, v2.position.y));
			}

			// Calculate reflected position and color.
			ReflectVertex (ref v0);
			ReflectVertex (ref v1);
			ReflectVertex (ref v2);
			ReflectVertex (ref v3);

			// Reverse quad index.
			quad [0] = v1;
			quad [1] = v0;
			quad [2] = v3;
			quad [3] = v2;

			// Add reflected quad.
			UIVertexUtil.AddQuadToStream (quad, result);
		}

		float GetLerpFactor (float bottom, float top)
		{
			return (height + _rect.yMin - bottom) / (top - bottom);
		}

		void ReflectVertex (ref UIVertex vt)
		{
			var col = vt.color;
			var pos = vt.position;

			// Reflected color
			var factor = Mathf.Clamp01 ((pos.y - _rect.yMin) / height);
			col *= Color.Lerp (m_StartColor, m_EndColor, factor);

			// Reflected position.
			pos.y = _rect.yMin * 2 - m_Space - pos.y;
			vt.position = pos;
			vt.color = col;
		}
	}
}