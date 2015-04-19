using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

namespace Light2D
{
    /// <summary>
    /// Used to draw lights. Puts LightOrigin world position to UV1.
    /// Supports Point and Line light types.
    /// </summary>
    [ExecuteInEditMode]
    public class LightSprite : CustomSprite
    {
        public Vector2 LightOrigin = Vector2.zero;
        public LightShape Shape = LightShape.Point;
        private Matrix4x4 _modelMatrix;
        private Vector2 _oldLightOrigin;
        private LightShape _oldLightShape;

        /// <summary>
        /// Update UV1 which is used for raytracking in shader. UV1 is set to world position of LightOrigin.
        /// </summary>
        private void UpdatePosition()
        {
            if (Sprite == null || !Application.isPlaying)
                return;

            var mat = _modelMatrix;
            Vector2 size = Sprite.bounds.size;

            if (Shape == LightShape.Point)
            {
                // LightOrigin needs to be send in world position instead of local because 
                // Unity non uniform scaling is breaking model matrix in shader.
                var pos = mat.MultiplyPoint(LightOrigin.Mul(size)); 
                for (int i = 0; i < _uv1.Length; i++)
                    _uv1[i] = pos;
            }
            else if (Shape == LightShape.Line)
            {
                var lpos = mat.MultiplyPoint(new Vector2(-0.5f, LightOrigin.y).Mul(size));
                var rpos = mat.MultiplyPoint(new Vector2(0.5f, LightOrigin.y).Mul(size));
                _uv1[0] = lpos;
                _uv1[1] = rpos;
                _uv1[2] = lpos;
                _uv1[3] = rpos;
            }
        }

        protected override void UpdateMeshData(bool forceUpdate = false)
        {
            if (IsPartOfStaticBatch)
                return;

            var objMat = transform.localToWorldMatrix;
            if (!objMat.FastEquals(_modelMatrix) ||
                _oldLightOrigin != LightOrigin || _oldLightShape != Shape || forceUpdate)
            {
                _modelMatrix = objMat;
                _oldLightOrigin = LightOrigin;
                _oldLightShape = Shape;
                UpdatePosition();
                _isMeshDirty = true;
            }

            base.UpdateMeshData(forceUpdate);
        }

        public enum LightShape
        {
            Point,
            Line,
        }

        private void OnDrawGizmosSelected()
        {
            if (Sprite == null)
                return;

            var size = Sprite.bounds.size;
            if (Shape == LightShape.Point)
            {
                var center = transform.TransformPoint(LightOrigin);
                Gizmos.DrawLine(
                    center + transform.TransformDirection(new Vector2(-0.1f, 0)),
                    center + transform.TransformDirection(new Vector2(0.1f, 0)));
                Gizmos.DrawLine(
                    center + transform.TransformDirection(new Vector2(0, -0.1f)),
                    center + transform.TransformDirection(new Vector2(0, 0.1f)));
            }
            else if (Shape == LightShape.Line && Sprite != null)
            {
                var lpos = transform.TransformPoint(new Vector3(-0.5f, LightOrigin.y).Mul(size));
                var rpos = transform.TransformPoint(new Vector3(0.5f, LightOrigin.y).Mul(size));
                Gizmos.DrawLine(lpos, rpos);
            }
        }
    }
}

