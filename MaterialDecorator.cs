using System.Collections.Generic;
using UnityEngine;

namespace PMC.Shop
{
    public class MaterialDecorator
    {
        private Dictionary<Material, Material> replacedStandardMaterials = new Dictionary<Material, Material>();
        private Material standard;
        private Material diffuse;
        private Material colorDiffuse;
        private Material specular;
        private Material colorSpecular;
        private Material colorDiffuseIllum;
        private Material colorSpecularIllum;
        private Material colorDiffuseTransparent;
        private Material colorSpecularTransparent;

        public MaterialDecorator()
        {
            foreach (Material objectMaterial in ScriptableSingleton<AssetManager>.Instance.objectMaterials)
            {
                switch (objectMaterial.name)
                {
                    case "CustomColorsDiffuse":
                        this.colorDiffuse = objectMaterial;
                        break;
                    case "CustomColorsIllum":
                        this.colorDiffuseIllum = objectMaterial;
                        break;
                    case "CustomColorsIllumSpecular":
                        this.colorSpecularIllum = objectMaterial;
                        break;
                    case "CustomColorsSpecular":
                        this.colorSpecular = objectMaterial;
                        break;
                    case "CustomColorsSpecularTransparent":
                        this.colorSpecularTransparent = objectMaterial;
                        break;
                    case "CustomColorsTransparent":
                        this.colorDiffuseTransparent = objectMaterial;
                        break;
                    case "Diffuse":
                        this.diffuse = objectMaterial;
                        break;
                    case "Specular":
                        this.specular = objectMaterial;
                        break;
                }
            }
        }

        public void replaceMaterials(GameObject go)
        {
            foreach (Renderer componentsInChild in go.GetComponentsInChildren<Renderer>())
            {
                Material[] sharedMaterials = componentsInChild.sharedMaterials;
                for (int index = 0; index < sharedMaterials.Length; ++index)
                {
                    Material material1 = sharedMaterials[index];
                    if (!((Object) material1 == (Object) null))
                    {
                        if (material1.name.StartsWith("Diffuse"))
                            sharedMaterials[index] = this.diffuse;
                        else if (material1.name.StartsWith("CustomColorsDiffuseTransparent"))
                            sharedMaterials[index] = this.colorDiffuseTransparent;
                        else if (material1.name.StartsWith("CustomColorsDiffuseIllum"))
                            sharedMaterials[index] = this.colorDiffuseIllum;
                        else if (material1.name.StartsWith("CustomColorsDiffuse"))
                            sharedMaterials[index] = this.colorDiffuse;
                        else if (material1.name.StartsWith("Specular"))
                            sharedMaterials[index] = this.specular;
                        else if (material1.name.StartsWith("CustomColorsSpecularTransparent"))
                            sharedMaterials[index] = this.colorSpecularTransparent;
                        else if (material1.name.StartsWith("CustomColorsSpecularIllum"))
                            sharedMaterials[index] = this.colorSpecularIllum;
                        else if (material1.name.StartsWith("CustomColorsSpecular"))
                            sharedMaterials[index] = this.colorSpecular;
                        else if (!material1.name.StartsWith("SignTextMaterial") &&
                                 !material1.name.StartsWith("tv_image") && !material1.name.StartsWith("ImageBanner"))
                        {
                            Material material2 = (Material) null;
                            if (!this.replacedStandardMaterials.TryGetValue(material1, out material2))
                            {
                                material2 = new Material(ScriptableSingleton<AssetManager>.Instance.standardShader);
                                material2.CopyPropertiesFromMaterial(material1);
                                material2.enableInstancing = true;
                                this.replacedStandardMaterials.Add(material1, material2);
                            }

                            sharedMaterials[index] = material2;
                        }
                    }
                }

                componentsInChild.sharedMaterials = sharedMaterials;
            }
        }
    }
}
