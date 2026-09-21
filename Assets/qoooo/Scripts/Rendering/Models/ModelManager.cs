using System;
using System.Collections.Generic;
using System.Linq;
using qoooo.Parameters.Model;
using qoooo.Parameters.Runtime;
using UnityEngine;

namespace qoooo.Rendering.Models
{
    public class ModelManager : MonoBehaviour, IMidiParameterContainer
    {
        [SerializeField] private List<GameObject> _models = new();

        [SerializeField] private RadioParameter _source = new(
            "model.source",
            "Model Source",
            new[]
            {
                "ArmchairSlopeReplica001", "Candles001_CandleMelted02_Geo", "FoodCinnamonRolls001",
                "FoodGrapes001_Collection01_Geo", "BreadBaguetteFrenchSlice001", "Headphones001",
                "JugCeramicGlazed001", "Microwave002_Geo", "PlantAgave002", "PlantBanjoFig001",
                "PlantOrchid001", "PlantZZ001", "SinkElevated002", "SofaReversiReplica001",
                "ToiletModern001", "UtensilsJar001", "WallLanternExterior001_Geo"
            });

        private int _activeSource = -1;

        public GameObject CurrentModel { get; private set; }
        public event Action<GameObject> CurrentModelChanged;

        private void Awake()
        {
            SynchronizeSourceOptions();
        }

        private void Start()
        {
            ApplySource(_source.Value);
        }

        private void Update()
        {
            if (_activeSource != _source.Value)
                ApplySource(_source.Value);
        }

        private void OnValidate()
        {
            SynchronizeSourceOptions();
        }

        public IEnumerable<IMidiBindableParameter> MidiParameters
        {
            get { yield return _source; }
        }

        private void SynchronizeSourceOptions()
        {
            _source ??= new RadioParameter(
                "model.source",
                "Model Source",
                new string[0]);

            var models = _models ?? new List<GameObject>();

            _source.ReplaceOptions(
                models.Select((model, index) =>
                    model != null ? model.name : $"Missing Model {index}"));
        }

        private void ApplySource(int sourceIndex)
        {
            var models = _models ?? new List<GameObject>();
            if (models.Count == 0)
            {
                CurrentModel = null;
                _activeSource = -1;
                CurrentModelChanged?.Invoke(CurrentModel);
                return;
            }

            var selectedIndex = Mathf.Clamp(sourceIndex, 0, Mathf.Max(0, models.Count - 1));
            CurrentModel = models[selectedIndex];
            for (var index = 0; index < models.Count; index++)
                if (models[index] != null)
                    models[index].SetActive(index == selectedIndex);

            _activeSource = selectedIndex;
            CurrentModelChanged?.Invoke(CurrentModel);
        }
    }
}
