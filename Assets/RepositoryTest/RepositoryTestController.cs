using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Lilja.Repository;
using RepositoryTest.Repositories;
using UnityEngine;
using UnityEngine.UI;

namespace RepositoryTest
{
    /// <summary>
    /// Lilja.Repositoryのテスト用UIコントローラ。
    /// </summary>
    public class RepositoryTestController : MonoBehaviour
    {
        [SerializeField]
        private Dropdown _storageTypeDropdown;

        [SerializeField]
        private InputField _idInput;

        [SerializeField]
        private InputField _nameInput;

        [SerializeField]
        private InputField _levelInput;

        [SerializeField]
        private InputField _posXInput;

        [SerializeField]
        private InputField _posYInput;

        [SerializeField]
        private Button _createButton;

        [SerializeField]
        private Button _readButton;

        [SerializeField]
        private Button _updateButton;

        [SerializeField]
        private Button _deleteButton;

        [SerializeField]
        private Text _logText;

        [SerializeField]
        private Text _listText;

        [SerializeField]
        private Text _listHeaderText;

        private IMonsterRepository _repository;
        private TxManager _txManager;

        private void Start()
        {
            _txManager = new TxManager();

            _storageTypeDropdown.ClearOptions();
            _storageTypeDropdown.AddOptions(new System.Collections.Generic.List<string> { "Json", "MessagePack" });
            _storageTypeDropdown.onValueChanged.AddListener(_ => OnStorageTypeChangedAsync(destroyCancellationToken).Forget());

            _createButton.onClick.AddListener(() => OnCreateAsync(destroyCancellationToken).Forget());
            _readButton.onClick.AddListener(() => OnReadAsync(destroyCancellationToken).Forget());
            _updateButton.onClick.AddListener(() => OnUpdateAsync(destroyCancellationToken).Forget());
            _deleteButton.onClick.AddListener(() => OnDeleteAsync(destroyCancellationToken).Forget());

            InitializeRepositoryAsync(0, destroyCancellationToken).Forget();
        }

        /// <summary>
        /// ストレージタイプ変更時の処理。
        /// </summary>
        private async UniTask OnStorageTypeChangedAsync(CancellationToken ct)
        {
            await InitializeRepositoryAsync(_storageTypeDropdown.value, ct);
        }

        /// <summary>
        /// リポジトリの初期化。
        /// </summary>
        private async UniTask InitializeRepositoryAsync(int storageType, CancellationToken ct)
        {
            try
            {
                if (storageType == 0)
                {
                    var jsonRepo = new JsonMonsterRepository();
                    await jsonRepo.InitializeAsync(ct);
                    _repository = jsonRepo;
                    SetLog("Json リポジトリを初期化しました。");
                }
                else
                {
                    var msgpackRepo = new MessagePackMonsterRepository();
                    await msgpackRepo.InitializeAsync(ct);
                    _repository = msgpackRepo;
                    SetLog("MessagePack リポジトリを初期化しました。");
                }

                RefreshList();
            }
            catch (Exception ex)
            {
                SetLog($"初期化エラー: {ex.Message}");
            }
        }

        /// <summary>
        /// Create操作。
        /// </summary>
        private async UniTask OnCreateAsync(CancellationToken ct)
        {
            if (!TryParseInput(out var id, out var name, out var level, out var position))
            {
                return;
            }

            try
            {
                var monster = new Monster(id, name, level, position);
                await _txManager.BeginRWTransactionAsync(tx =>
                {
                    _repository.Create(tx, monster);
                }, ct);
                SetLog($"作成: Id={id}, Name={name}, Level={level}, Position={position}");
                RefreshList();
            }
            catch (Exception ex)
            {
                SetLog($"作成エラー: {ex.Message}");
            }
        }

        /// <summary>
        /// Read操作。
        /// </summary>
        private async UniTask OnReadAsync(CancellationToken ct)
        {
            if (!int.TryParse(_idInput.text, out var id))
            {
                SetLog("IDを正しく入力してください。");
                return;
            }

            try
            {
                Monster monster = null;
                _txManager.BeginROTransaction(tx =>
                {
                    monster = _repository.Read(tx, id);
                });

                if (monster != null)
                {
                    _nameInput.text = monster.Name;
                    _levelInput.text = monster.Level.ToString();
                    _posXInput.text = monster.Position.X.ToString();
                    _posYInput.text = monster.Position.Y.ToString();
                    SetLog($"読取: Id={monster.Id}, Name={monster.Name}, Level={monster.Level}, Position={monster.Position}");
                }
                else
                {
                    SetLog($"Id={id} のモンスターは存在しません。");
                }

                await UniTask.CompletedTask;
            }
            catch (Exception ex)
            {
                SetLog($"読取エラー: {ex.Message}");
            }
        }

        /// <summary>
        /// Update操作。
        /// </summary>
        private async UniTask OnUpdateAsync(CancellationToken ct)
        {
            if (!TryParseInput(out var id, out var name, out var level, out var position))
            {
                return;
            }

            try
            {
                var monster = new Monster(id, name, level, position);
                await _txManager.BeginRWTransactionAsync(tx =>
                {
                    _repository.Update(tx, monster);
                }, ct);
                SetLog($"更新: Id={id}, Name={name}, Level={level}, Position={position}");
                RefreshList();
            }
            catch (Exception ex)
            {
                SetLog($"更新エラー: {ex.Message}");
            }
        }

        /// <summary>
        /// Delete操作。
        /// </summary>
        private async UniTask OnDeleteAsync(CancellationToken ct)
        {
            if (!int.TryParse(_idInput.text, out var id))
            {
                SetLog("IDを正しく入力してください。");
                return;
            }

            try
            {
                await _txManager.BeginRWTransactionAsync(tx =>
                {
                    _repository.Delete(tx, id);
                }, ct);
                SetLog($"削除: Id={id}");
                RefreshList();
            }
            catch (Exception ex)
            {
                SetLog($"削除エラー: {ex.Message}");
            }
        }

        /// <summary>
        /// 全件一覧を更新する。
        /// </summary>
        private void RefreshList()
        {
            try
            {
                System.Collections.Generic.IReadOnlyList<Monster> monsters = null;
                _txManager.BeginROTransaction(tx =>
                {
                    monsters = _repository.All(tx);
                });

                if (monsters == null || monsters.Count == 0)
                {
                    _listHeaderText.text = "一覧 (0件)";
                    _listText.text = "データなし";
                    return;
                }

                _listHeaderText.text = $"一覧 ({monsters.Count}件)";
                var sb = new System.Text.StringBuilder();
                foreach (var m in monsters)
                {
                    sb.AppendLine($"[{m.Id}] {m.Name}  Lv.{m.Level}  Pos{m.Position}");
                }

                _listText.text = sb.ToString();
            }
            catch (Exception ex)
            {
                _listText.text = $"一覧取得エラー: {ex.Message}";
            }
        }

        /// <summary>
        /// 入力フィールドをパースする。
        /// </summary>
        private bool TryParseInput(out int id, out string name, out int level, out Position position)
        {
            id = 0;
            name = string.Empty;
            level = 0;
            position = default;

            if (!int.TryParse(_idInput.text, out id))
            {
                SetLog("IDを正しく入力してください。");
                return false;
            }

            name = _nameInput.text;
            if (string.IsNullOrEmpty(name))
            {
                SetLog("名前を入力してください。");
                return false;
            }

            if (!int.TryParse(_levelInput.text, out level))
            {
                SetLog("レベルを正しく入力してください。");
                return false;
            }

            if (!int.TryParse(_posXInput.text, out var posX) || !int.TryParse(_posYInput.text, out var posY))
            {
                SetLog("座標を正しく入力してください。");
                return false;
            }

            position = new Position(posX, posY);
            return true;
        }

        /// <summary>
        /// ログを設定する。
        /// </summary>
        private void SetLog(string message)
        {
            Debug.Log($"[RepositoryTest] {message}");
            if (_logText != null)
            {
                _logText.text = message;
            }
        }
    }
}
