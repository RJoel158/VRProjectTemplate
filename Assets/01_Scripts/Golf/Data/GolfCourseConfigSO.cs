using System;
using System.Collections.Generic;
using UnityEngine;

namespace Golf.Data
{
    /// <summary>
    /// Configuracion del circuito completo de Minigolf.
    /// Agrupa la lista ordenada de hoyos que componen el torneo o partida.
    /// </summary>
    [CreateAssetMenu(fileName = "GolfCourseConfig", menuName = "VR Sports/Golf/Course Config")]
    public class GolfCourseConfigSO : ScriptableObject
    {
        [Header("Course Info")]
        [SerializeField] private string courseName = "Resort Island Minigolf";
        [TextArea(2, 4)]
        [SerializeField] private string description = "Circuito costero de 3 hoyos con curvas de madera, rampas y molino giratorio.";

        [Header("Holes in Circuit")]
        [SerializeField] private List<HoleDataSO> holes = new List<HoleDataSO>();

        public string CourseName => courseName;
        public string Description => description;
        public List<HoleDataSO> Holes => holes;

        public int TotalHoles => holes != null ? holes.Count : 0;

        public int TotalPar
        {
            get
            {
                if (holes == null) return 0;
                int sum = 0;
                for (int i = 0; i < holes.Count; i++)
                {
                    if (holes[i] != null) sum += holes[i].Par;
                }
                return sum;
            }
        }
    }
}
