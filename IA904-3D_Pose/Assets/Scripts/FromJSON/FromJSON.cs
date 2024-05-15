using System;
using System.IO;
using System.Reflection;
using UnityEngine;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SFB;

#if UNITY_EDITOR
    using UnityEditor;
#endif


namespace HIAAC.FromJSON
{
    /// <summary>
    /// Converts JSON to ScriptableObjects
    /// </summary>
    public class FromJSON
    {
        #if UNITY_EDITOR

            /// <summary>
            /// Creates an ScriptableObject from TextAsset.
            /// </summary>
            [MenuItem("Assets/Create/JSON/Scriptable Object from JSON", false, 0)]
            static void SOFromTextAsset()
            {
                foreach(UnityEngine.Object obj in Selection.objects)
                {
                    TextAsset text = (TextAsset) obj;
                    string json = text.ToString();

                    ScriptableObject so = SOFromJSON(json);

                    string path = Path.GetDirectoryName(AssetDatabase.GetAssetPath(Selection.activeObject.GetInstanceID())); 
                    path += "/"+text.name+".asset";

                    AssetDatabase.CreateAsset(so, path);
                }
                
                AssetDatabase.SaveAssets();
            }
            
            /// <summary>
            /// Validates if the select file is a TextAsset to convert to ScriptableObject.
            /// </summary>
            /// <returns>True if the activeObject is a TextAsset.</returns>
            [MenuItem("Assets/Create/JSON/Scriptable Object from JSON", true)]
            static bool SOFromTextAssetValidator()
            {
                foreach(UnityEngine.Object obj in Selection.objects)
                {
                    TextAsset test = Selection.activeObject as TextAsset;
                    if(test == null)
                    {
                        return false;
                    }
                }

                return true;
            }
        #endif

        /// <summary>
        /// Creates an ScriptableObject from a JSON file path.
        /// </summary>
        /// <param name="path">JSON file path.</param>
        /// <param name="save">If should save the object to the project assets (Editor only).</param>
        /// <returns>Created ScriptableObject</returns>
        public static ScriptableObject SOFromFilePath(string path, bool save=true)
        {
            StreamReader reader = new StreamReader(path);
            string json = reader.ReadToEnd();
            reader.Close();

            ScriptableObject so = SOFromJSON(json);
            
            #if UNITY_EDITOR
                string soPath = "Assets/"+Path.GetFileNameWithoutExtension(path)+".asset";
                AssetDatabase.CreateAsset(so, soPath);

                if(save)
                {
                    AssetDatabase.SaveAssets();
                }
            #endif

            return so;
        }

        /// <summary>
        /// Creates multiples ScriptableObjects from JSON files paths.
        /// </summary>
        /// <param name="path">JSON files paths.</param>
        /// <param name="save">If should save the objects to the project assets (Editor only).</param>
        /// <returns>Created ScriptableObjects</returns>
        public static ScriptableObject[] SOFromFilePath(string[] paths, bool save=true)
        {
            ScriptableObject[] objects = new ScriptableObject[paths.Length];
            
            for(int i = 0; i<paths.Length; i++)
            {
                objects[i] = SOFromFilePath(paths[i], false);
            }
            #if UNITY_EDITOR
                AssetDatabase.SaveAssets();
            #endif

            return objects;
        }

        /// <summary>
        /// Creates an ScriptableObject from a string with json.
        /// </summary>
        /// <param name="json">String with json object encoding.</param>
        /// <returns></returns>
        /// <exception cref="ArgumentException">If the json data doesn't contains 'type' field.</exception>
        public static ScriptableObject SOFromJSON(string json)
        {
            JObject data = JObject.Parse(json);
            
            if(!data.ContainsKey("type"))
            {
                throw new ArgumentException("JSON must contain a 'type' field with Scriptable Object type.");
            }

            string typeName = data["type"].Value<string>();

            ScriptableObject obj = ScriptableObject.CreateInstance(typeName);

            //JsonConvert.PopulateObject(json, obj);
            JsonUtility.FromJsonOverwrite(json, obj);

            return obj;
        }

        public delegate void OnLoad(ScriptableObject[] objs);

        /// <summary>
        /// Asks the user to select a JSON file and creates a SO.
        /// </summary>
        /// <param name="callback">Function to call when the object is created (must be of <see cref="FromJSON.OnLoad"> OnLoad </see> type)</param>
        /// <param name="allowMultiple">If should allow the user to select multiple files.</param>
        public static void askSO(OnLoad callback, bool allowMultiple=false)
        {
            StandaloneFileBrowser.OpenFilePanelAsync("Select JSONs", "", "json", allowMultiple, (paths) => {onFileSelect(callback, paths);});
        }

        /// <summary>
        /// Internal callback for when the user select the JSON files.
        /// </summary>
        /// <param name="callback">Caller callback to invoke after creating the objects.</param>
        /// <param name="paths">Paths of the JSON files.</param>
        static void onFileSelect(OnLoad callback, string[] paths)
        {
            ScriptableObject[] objects = SOFromFilePath(paths);
            callback.Invoke(objects);
        } 

    }
}