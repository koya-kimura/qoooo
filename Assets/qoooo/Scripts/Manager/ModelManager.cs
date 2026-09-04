using System;
using System.Collections.Generic;
using UnityEngine;

namespace qoooo.Manager
{
    public class ModelManager : MonoBehaviour
    {
        [SerializeField] private List<GameObject> _models;

        private void Start()
        {
            Instantiate(_models[0],  transform);
        }
    }
}
