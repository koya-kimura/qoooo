using System.Collections.Generic;
using UnityEngine;

namespace qoooo.Rendering.Models
{
    public class ModelManager : MonoBehaviour
    {
        [SerializeField] private List<GameObject> _models;

        private void Start()
        {
            _models[0].SetActive(true);
        }
    }
}