using System;
using UdonSharp;
using VRDC_systems.Building.Scripts.Building;

namespace VRDC_systems.Building.Scripts.UI
{
    public class ToolInspector : UdonSharpBehaviour
    {
        private void Start()
        {
            GetComponentInParent<DesktopBuilderPage>(true).RegisterEditor(this);
        }

        public void SetActive(bool value)
        {
            gameObject.SetActive(value);
        }
        public virtual string AssociatedTool()
        {
            return string.Empty;
        }
        public virtual void SetData(BuildManager buildManager, Builder desktopBuilder, BuilderTool tool)
        {
            
        }
    }
}