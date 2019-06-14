using System;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;

namespace RLO.Science.BasicsOfProgramming
{
    public class ProgrammingLevel : MonoBehaviour
    {
        public List<Transform> TileTransforms;
        public float CamSize => _camSize;
        public Transform CamFocus => _camFocus;
        public Tile StartTile => _startTile;
        public int DatesToCollect => _datesToCollect;
        
        
        [SerializeField] private float _camSize;
        [SerializeField] private Transform _camFocus;
        [SerializeField] private Tile _startTile;
        [SerializeField] private Tile[] _tileControllers;
        [SerializeField] private Pickup[] _pickups;
        [SerializeField] private int _datesToCollect;
        

        public void SpawnLevel(Action onComplete = null)
        {
            foreach (var pickup in _pickups) pickup.gameObject.SetActive(false);

            var animDuration = 0.6f;
            var delayMod = 1.6f;
            var tileTforms = new List<Transform>(TileTransforms);
            var delays = AnimationSequenceDelay.Calculate(tileTforms, _startTile.transform);
            for (int i = 0; i < TileTransforms.Count; i++)
            {
                var tgtPos = tileTforms[i].localPosition;
                var from = new Vector3(tgtPos.x, tgtPos.y - 5, tgtPos.z);
                tileTforms[i].DOLocalMove(from, animDuration).From().SetDelay(delays[i]/delayMod).SetEase(Ease.OutCirc);
                var t = tileTforms[i].DOScale(Vector3.zero, animDuration).From().SetDelay(delays[i]/delayMod).SetEase(Ease.InSine);

                if (i == TileTransforms.Count - 1) t.OnComplete(() =>
                {
                    foreach (var pickup in _pickups)
                    {
                        pickup.gameObject.SetActive(true);
                        pickup.Spawn();
                    }

                    onComplete?.Invoke();
                });
            }
        }
        
        public void DespawnLevel(Action onComplete = null)
        {
            var split = .03f;
            var stuff = new List<Transform>(TileTransforms).Shuffle();

            for (int i = 0; i < TileTransforms.Count; i++)
            {
                var t = stuff[i].DOScale(Vector3.zero, .4f).SetDelay(i * split).SetEase(Ease.InSine);
                
                if (i == TileTransforms.Count - 1) t.OnComplete(() => onComplete?.Invoke());
            }
        }

        
        public void RestartLevel(Action onComplete = null)
        {
            var tiles = new List<Transform>(TileTransforms);
            var delays = AnimationSequenceDelay.Calculate(tiles, _startTile.transform);
            
            foreach(var pickup in _pickups) pickup.OnPreReset();

            for (int i = 0; i < tiles.Count; i++)
            {
                var t = tiles[i].DOPunchPosition(-Vector3.up / 4, .25f, 2, .5f)
                    .SetDelay(delays[i]/3)
                    .SetEase(Ease.InOutSine);

                if (i == tiles.Count - 1) t.OnComplete(() =>
                {
                    foreach (var pickup in _pickups) pickup.Spawn();
                    onComplete?.Invoke();
                });
            }
        }
        
        [Button(ButtonSizes.Large, Name = "Set names, links and references")]
        private void DEV_SetupLevel()
        {
            _tileControllers = GetComponentsInChildren<Tile>();
            TileTransforms = new List<Transform>(_tileControllers.Select(x => x.transform));

            _pickups = GetComponentsInChildren<Pickup>();

            var totalTiles = TileTransforms.Count;
            var tileList = new List<Tile>();

            void VisitTile(Tile tile, ref List<Tile> visited, int limit)
            {
                if (visited.Contains(tile)) return;
                visited.Add(tile);
                if (visited.Count == limit) return;
                
                tile.DetectNeighbours();

                var n = tile.Links[DIR.North];
                var s = tile.Links[DIR.South];
                var e = tile.Links[DIR.East];
                var w = tile.Links[DIR.West];
                
                if (n) VisitTile(n, ref visited, limit);
                if (s) VisitTile(s, ref visited, limit);
                if (e) VisitTile(e, ref visited, limit);
                if (w) VisitTile(w, ref visited, limit);
            }
            
            VisitTile(_tileControllers[0], ref tileList, totalTiles);

            for (int i = 0; i < tileList.Count; i++)
            {
                var idString = i.ToString();
                for (int j = idString.Length; j < 3; j++)
                {
                    idString = "0" + idString;
                }
                
                tileList[i].gameObject.name = $"Tile_{idString}_{gameObject.name}";
            }
        }

        private static class AnimationSequenceDelay
        {
            private static float xFactor = 0.09f;
            private static float zFactor = 0.15f;
            public static float[] Calculate(List<Transform> items, Transform center)
            {
                var result = new float[items.Count];
                var reference = center.position;

                for (int i = 0; i < items.Count; i++)
                {
                    var itemPos = items[i].position;
                    var diff = reference - itemPos;
                    var delay = Mathf.Abs(diff.z * zFactor) + Mathf.Abs(diff.x * xFactor);

                    result[i] = delay;
                }

                return result;
            }
        }
    }
}