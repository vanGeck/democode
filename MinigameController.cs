using System;
using System.Collections.Generic;
using System.Text;
using Sirenix.OdinInspector;
using UnityEngine;

namespace RLO.Science.BasicsOfProgramming
{
    public class MinigameController : MonoBehaviour
    {
        public Action OnLevelComplete = delegate { };
        public Action<ERROR> OnLevelFailed = delegate { };

        [SerializeField] private InstructionContainer _container;
        [SerializeField] private GameObject _monobot;
        [SerializeField] private MonobotController _botController;
        [SerializeField] private ProgrammingLevel[] _levels;
        
        private ProgrammingLevel _currentLevel;
        private int _currentLevelId;
        private bool _bananaBoost;
        private bool _stopExecution;
        private GameState _gameState;
        
        private void Start()
        {
            _botController.OnItemPickup += HandleItemPickup;
            _monobot.transform.localScale = Vector3.zero;
        }

        public void BeginGame(Action onSpawnComplete)
        {
            SwitchLevel(0, onSpawnComplete, null);
        }

        public void NextLevel(Action onComplete, Action onFailure)
        {
            SwitchLevel(_currentLevelId + 1 ,onComplete, onFailure);
        }
        
        public void SwitchLevel(int level, Action onComplete, Action onFailure)
        {
            if (level > _levels.Length - 1)
            {
                onFailure?.Invoke();
                return;
            }

            _container.Clear();
            
            if (_currentLevel == null)
            {
                _currentLevel = _levels[level];
                _currentLevel.gameObject.SetActive(true);
                
                var startTile = _currentLevel.StartTile;
                
                _currentLevel.SpawnLevel(() =>
                {
                    onComplete?.Invoke();
                    _botController.SpawnBot(null, startTile);
                });
            }
            else
            {
                _currentLevel.DespawnLevel(() =>
                {
                    _currentLevel.gameObject.SetActive(false);
                
                    _currentLevel = _levels[level];
                    _currentLevel.gameObject.SetActive(true);

                    var startTile = _currentLevel.StartTile;
                
                    _currentLevel.SpawnLevel(() =>
                    {
                        onComplete?.Invoke();
                        _botController.SpawnBot(null, startTile);
                    });
                });
            }

            _currentLevelId = level;
            _gameState = new GameState {DatesLeft = _currentLevel.DatesToCollect};
        }
        
        public void RestartLevel(Action onComplete)
        {
            _gameState = new GameState {DatesLeft = _currentLevel.DatesToCollect};
            
            _botController.DespawnBot(() =>
            {
                _currentLevel.RestartLevel(() =>
                {
                    _botController.SpawnBot(null, _currentLevel.StartTile);
                    onComplete?.Invoke();
                });
            });
        }

        public void BeginProgram()
        {
            var instructions = _container.GetInstructions();
            if (instructions.Count == 0)
            {
                Debug.Log("Instruction queue empty!");
                return;
            }

            _stopExecution = false;

            Action<ERROR> onFail = x => { Debug.Log($"Dun' goofed: {x}"); };
            Action onSuccess = HandleProgramCompletion;
            ExecuteInstruction(instructions, onFail, onSuccess); // this is recursive
        }

        private void ExecuteInstruction(Queue<INSTRUCTION> q, Action<ERROR> onFailure, Action onComplete)
        {
            if (_stopExecution)
            {
                _stopExecution = false;
                return;
            }
            
            if (_bananaBoost)
            {
                // The boost adds 3 forward movements to program.
                var x = new List<INSTRUCTION>(q);
                x.Insert(0, INSTRUCTION.Forward);
                x.Insert(0, INSTRUCTION.Forward);
                x.Insert(0, INSTRUCTION.Forward);
                q = new Queue<INSTRUCTION>(x);
                _bananaBoost = false;
            }
            
            if (q.Count == 0)
            {
                // Program complete!
                onComplete?.Invoke();
                return;
            }
            
            // TODO: Debug helper, remove on deployment.
            StringBuilder sb = new StringBuilder();
            sb.Append("Instruction queue: ");
            foreach (var i in q)
            {
                sb.Append(i + ", ");
            }
            Debug.Log(sb.ToString());
            
            var instruction = q.Dequeue();
            Debug.Log($"Executing {instruction}");
            
            // END OF DEBUG MSG
            
            Action success = () => ExecuteInstruction(q, onFailure, onComplete);
            Action fail = () => onFailure.Invoke(ERROR.OOB);
            
            if (instruction == INSTRUCTION.Forward)
            {
                _botController.MoveForward(success, fail);
            }
            else
            {
                _botController.Turn(instruction == INSTRUCTION.Right, success);
            }
        }

        private void HandleItemPickup(ITEM item)
        {
            switch (item)
            {
                // check for basket
                case ITEM.Date when _gameState.DatesPickedUp > 0 && !_gameState.BasketAcquired:
                    Debug.Log("Monobot tried to picked up more than 1 date, but had no basket.");
                    _stopExecution = true;
                    return;
                
                case ITEM.Date:
                    _gameState.DatesLeft--;
                    _gameState.DatesPickedUp++;
                    if (_gameState.DatesLeft == 0)
                    {
                        _gameState.VictoryAchieved = true;
                    }
                    break;
                
                case ITEM.Basket:
                    _gameState.BasketAcquired = true;
                    break;
                
                case ITEM.Banana:
                    _bananaBoost = true;
                    break;
            }
        }

        private void HandleProgramCompletion()
        {
            if (!_gameState.VictoryAchieved)
                OnLevelFailed.Invoke(ERROR.NoVictory);
            else
                OnLevelComplete.Invoke();
        }

        // TODO: Remove on deployment.
        [Button(ButtonSizes.Small)]
        public void DEV_SpawnNextLevel()
        {
            _botController.DespawnBot(() =>
            {
                SwitchLevel(_currentLevelId + 1 ,null, null);
            }); 
        }

        private struct GameState
        {
            public int DatesLeft;
            public int DatesPickedUp;
            public bool BasketAcquired;
            public bool VictoryAchieved;
        }
    }
    
    
    public enum ERROR { OOB, NoBasket, NoVictory}
}