using UnityEngine;
using UnityEngine.Serialization;

namespace Restock
{
    public class ModuleDepthMask : PartModule
    {
        // The name of the transform that has your mask mesh. The only strictly required property
        [KSPField] 
        public string maskTransform = "";

        [KSPField] 
        public string bodyTransform = "";

        // The name of the depth mask shader
        [KSPField] 
        public string shaderName = "DepthMask";

        // The render queue value for the mesh, should be less than maskRenderQueue
        [KSPField] 
        public int meshRenderQueue = 1000;

        // the render queue value for the mask, should be less than 2000
        [KSPField] 
        public int maskRenderQueue = 1999;


        // depth mask object transform
        public Transform maskTransformObject;

        // body object transform
        public Transform bodyTransformObject;

        // depth mask shader object
        public Shader depthShader;


        public override void OnStart(StartState state)
        {
            base.OnStart(state);
            UpdatematerialQueue();

            // the part variant system is implemented extremely stupidly
            // so we have to make this whole module more complicated as a result
            GameEvents.onVariantApplied.Add(OnVariantApplied);
        }


        private void OnDestroy()
        {
            GameEvents.onVariantApplied.Remove(OnVariantApplied);
        }


        public override void OnLoad(ConfigNode node)
        {
            base.OnLoad(node);

            if (HighLogic.LoadedSceneIsEditor || HighLogic.LoadedSceneIsFlight) return;

            this.maskTransformObject = base.part.FindModelTransform(maskTransform);
            if (this.maskTransformObject == null)
            {
                this.LogError($"Can't find transform {maskTransform}");
                return;
            }

            if (bodyTransform == "")
            {
                this.bodyTransformObject = base.part.partTransform;
            }
            else
            {
                this.bodyTransformObject = base.part.FindModelTransform(bodyTransform);
                if (this.bodyTransformObject == null)
                {
                    this.LogError($"Can't find transform {bodyTransform}");
                    this.bodyTransformObject = base.part.partTransform;
                }
            }

            this.depthShader = Shader.Find(shaderName);
            if (this.depthShader == null)
            {
                this.LogError($"Can't find shader {shaderName}");
                return;
            }
        }


        public void OnVariantApplied(Part appliedPart, PartVariant variant)
        {
            // I dont know why changing part variants resets all the materials to their as-loaded state, but it does
            if (appliedPart == this.part) UpdatematerialQueue();
        }


        private void UpdatematerialQueue()
        {
            var windowRenderer = maskTransformObject.GetComponent<MeshRenderer>();

            windowRenderer.material.shader = depthShader;
            windowRenderer.material.renderQueue = maskRenderQueue;

            var meshRenderers = bodyTransformObject.GetComponentsInChildren<MeshRenderer>(true);
            var skinnedMeshRenderers = bodyTransformObject.GetComponentsInChildren<SkinnedMeshRenderer>(true);

            foreach (var renderer in meshRenderers)
            {
                if (renderer == windowRenderer) continue;
                var queue = renderer.material.renderQueue;
                queue = meshRenderQueue + ((queue - 2000) / 2);
                renderer.material.renderQueue = queue;
            }

            foreach (var renderer in skinnedMeshRenderers)
            {
                if (renderer == windowRenderer) continue;
                var queue = renderer.material.renderQueue;
                queue = meshRenderQueue + ((queue - 2000) / 2);
                renderer.material.renderQueue = queue;
            }
        }

        private void LogError(string message)
        {
            Debug.LogError($"[{part.partInfo?.name ?? part.name} {this.GetType()}] {message}");
        }
    }
}