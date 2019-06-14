using System;
using System.Collections;
using DG.Tweening;
using UnityEngine;

namespace RLO.Science.BasicsOfProgramming
{
    public class MonobotController : MonoBehaviour
    {
        public Tile CurrentTile;

        public Action<ITEM> OnItemPickup = delegate { };

        private DIR _heading = DIR.South;
        private Vector3 _turnLeft = new Vector3(0,-90, 0);
        private Vector3 _turnRight = new Vector3(0,90, 0);

        private bool _isMoving;
        private Coroutine _highlightCoroutine;

        private void OnEnable()
        {
            _highlightCoroutine = StartCoroutine(TileHighlightCoroutine());
        }

        private void OnDisable()
        {
            StopCoroutine(_highlightCoroutine);
        }

        private void OnTriggerEnter(Collider other)
        {
            var pickup = other.gameObject.GetComponent<Pickup>();
            if (!pickup) return;
            OnItemPickup.Invoke(pickup.ItemType);
            Debug.Log($"Monobot picked up {pickup.ItemType}.");
            
            pickup.OnPickup();
        }
        
        public void DespawnBot(Action onComplete)
        {
            transform.DOBlendableLocalMoveBy(Vector3.up * 5, .5f);
            transform.DOScale(Vector3.zero, .5f).OnComplete(() => onComplete?.Invoke());
        }

        public void SpawnBot(Action onComplete, Tile spawnTile)
        {
            var startPos = spawnTile.BotPos;
            var t = transform;
            DOTween.Kill(t, true);
            t.localRotation = Quaternion.identity;
            t.position = startPos;
            t.localScale = Vector3.zero;
            _heading = DIR.South;
            CurrentTile = spawnTile;
            
            t.DOLocalMoveY(10, .5f).From();
            t.DOScale(Vector3.one, .5f).OnComplete(() => onComplete?.Invoke());

            var levelName = CurrentTile.transform.parent.name;
            Debug.Log($"Spawning on {CurrentTile.gameObject.name} from {levelName}.");
        }

        public void Turn(bool right, Action onComplete)
        {
            var turn = right ? _turnRight : _turnLeft;
            transform.DOBlendableRotateBy(turn, .3f)
                .SetEase(Ease.InOutCirc)
                .OnComplete(() => onComplete?.Invoke());

            switch (_heading)
            {
                case DIR.North:
                    _heading = right ? DIR.East : DIR.West;
                    break;
                case DIR.East:
                    _heading = right ? DIR.South : DIR.North;
                    break;
                case DIR.South:
                    _heading = right ? DIR.West : DIR.East;
                    break;
                case DIR.West:
                    _heading = right ? DIR.North : DIR.South;
                    break;
                default:
                    return;
            }
        }
        
        public void MoveForward(Action onComplete, Action onFailure)
        {
            Move(_heading, onComplete, onFailure);
        }
        
        private void Move(DIR dir, Action onSuccess, Action onFailure)
        {
            var nextTile = CurrentTile.Links[dir];

            if (!nextTile)
            {
                onFailure?.Invoke();
                var levelName = CurrentTile.transform.parent.name;
                Debug.Log($"No path {dir} from tile {CurrentTile.gameObject.name} from {levelName}.");
                return;
            }

            transform.DOMove(nextTile.BotPos, .4f).SetEase(Ease.InOutCirc)
                .OnComplete(() =>
                {
                    onSuccess?.Invoke();
                });
            CurrentTile = nextTile;
        }

        private IEnumerator TileHighlightCoroutine()
        {
            /* Used to highlight tiles on bot movement */
            var frame = new WaitForEndOfFrame();
            Tile previousTile = null;
            Tile currentTile = null;
            
            while (true)
            {
                Ray ray = new Ray(transform.position, Vector3.down);
                Physics.Raycast(ray, out var hit, 1.5f);
                yield return frame;
                if (!hit.collider) continue;
                var tile = hit.collider.GetComponent<Tile>();
                if (!tile) continue;
                if (currentTile != null && currentTile != tile)
                {
                    previousTile = currentTile;
                    previousTile.OnBotLeave();
                    currentTile = tile;
                    currentTile.OnBotVisit();
                }
                else if (currentTile == null)
                {
                    currentTile = tile;
                }
                
                // limit raycast frequency to minimize performance hit.
                yield return frame;
                yield return frame;
            }
        }
    }
    
    public enum ITEM { Date, Basket, Banana }
}