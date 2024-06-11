using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Perception.Randomization.Scenarios;
using UnityEngine.SceneManagement;

namespace IA904_3DPose
{
    public class DataManager : MonoBehaviour
    {
        [SerializeField] private LoadingPanel _loadingPanel;

        private const string SCENE_SCENARIO = "Main";
        private const string DATASET_INFO = "DatasetInfo.csv";
        private const string DATASET_2D = "Dataset2D.csv";
        private const string DATASET_3D = "Dataset3D.csv";

        public static DataManager Instance;

        public string Select_Path {  get; private set; }
        public string Selected_Scenario { get; private set; }

        private void Awake()
        {
            if (Instance == null)
                Instance = this;
            else
                Destroy(this);

            DontDestroyOnLoad(Instance);
        }

        public void GenerateDataset(string path, string scenario)
        {
            Select_Path = path;
            Selected_Scenario = scenario;
            StartCoroutine(LoadScenario());
        }

        public void BuildDataframe(string path)
        {
            Select_Path = path;
            StartCoroutine(BuildDataframeFile());
        }

        private IEnumerator LoadScenario()
        {
            var operation = SceneManager.LoadSceneAsync(SCENE_SCENARIO);

            _loadingPanel.StartLoad(0.9d);

            while (!operation.isDone)
            {
                _loadingPanel.UpdateProgress(operation.progress, $"Loading scene...");
                yield return null;
            }

            _loadingPanel.FinishLoad();
        }

        private IEnumerator BuildDataframeFile()
        {
            string[] scenarios = Directory.GetDirectories(Select_Path);
            int size = scenarios.Select(x => Directory.GetDirectories(x)).SelectMany(x => x).Count();
            int progress = 0;
            int randomUpdate = UnityEngine.Random.Range(10, 100);
            List<string> datasetInfo = new List<string>();
            List<string> dataset2D = new List<string>();
            List<string> dataset3D = new List<string>();

            _loadingPanel.StartLoad(size);

            foreach (var scenario in scenarios)
            {
                string scenarioName = Path.GetFileName(scenario);
                string[] sequences = Directory.GetDirectories(scenario);

                foreach (var sequence in sequences)
                {
                    string sequenceName = Path.GetFileName(sequence);
                    string json = File.ReadAllText(Directory.GetFiles(sequence, "*.json").FirstOrDefault());
                    FrameData frameData = JsonConvert.DeserializeObject<FrameData>(json);

                    if (frameData.sequence != 0)
                    {
                        if (dataset3D.Count == 0)
                        {
                            datasetInfo.Add(frameData.GetHead());
                            dataset2D.Add(frameData.GetHead2D());
                            dataset3D.Add(frameData.GetHead3D());
                        }

                        datasetInfo.Add($"{scenarioName},{frameData}");
                        dataset2D.AddRange(frameData.ToDataFrame2D(scenarioName));
                        dataset3D.AddRange(frameData.ToDataFrame3D(scenarioName));
                    }

                    progress++;
                    if (progress > randomUpdate)
                    {
                        randomUpdate = progress + UnityEngine.Random.Range(10, 100);
                        _loadingPanel.UpdateProgress(progress, $"{scenarioName} - Processed {sequenceName}");
                        yield return null;
                    }
                }

                _loadingPanel.UpdateProgress(progress, $"Processed {scenarioName}!");
                yield return null;
            }

            _loadingPanel.UpdateProgress(progress++, "Saving CSV...");

            File.WriteAllLines($"{Select_Path}/{DATASET_INFO}", datasetInfo);
            File.WriteAllLines($"{Select_Path}/{DATASET_2D}", dataset2D);
            File.WriteAllLines($"{Select_Path}/{DATASET_3D}", dataset3D);
            _loadingPanel.FinishLoad();
        }
    }
}
