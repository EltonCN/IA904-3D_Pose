using UnityEditor;
using UnityEngine;
using SFB;

namespace HIAAC.FromJSON
{
    public class FromJSONWizard : ScriptableWizard
    {
        public string[] filePaths;    
        
        [MenuItem("Tools/JSON/Scriptable objects from JSONs")]
        static void CreateWizard()
        {
            ScriptableWizard.DisplayWizard<FromJSONWizard>("Scriptable objects from JSONs", "Create", "Select");
        }

        void OnWizardUpdate()
        {
            if(filePaths == null)
            {
                //isValid = false;
            }


        } 

        void OnWizardOtherButton()
        {
            filePaths = StandaloneFileBrowser.OpenFilePanel("Select JSONs", "", "json", true);
        }

        void OnWizardCreate()
        {
            FromJSON.SOFromFilePath(filePaths, true);
        }
    }
}