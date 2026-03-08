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
    /// Lilja.RepositoryのHero（シングルトンEntity）用のテストUIコントローラ。
    /// </summary>
    public class HeroRepositoryTestController : MonoBehaviour
    {
        [SerializeField]
        private Dropdown _storageTypeDropdown;

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

        private IHeroRepository _repository;
        private TxManager _txManager;

        private void Start()
        {
            _txManager = new TxManager();

            if (_storageTypeDropdown != null)
            {
                _storageTypeDropdown.ClearOptions();
                _storageTypeDropdown.AddOptions(new System.Collections.Generic.List<string> { "Json", "MessagePack" });
                _storageTypeDropdown.onValueChanged.AddListener(_ => OnStorageTypeChangedAsync(destroyCancellationToken).Forget());
            }

            if (_createButton != null) _createButton.onClick.AddListener(() => OnCreateAsync(destroyCancellationToken).Forget());
            if (_readButton != null) _readButton.onClick.AddListener(() => OnReadAsync(destroyCancellationToken).Forget());
            if (_updateButton != null) _updateButton.onClick.AddListener(() => OnUpdateAsync(destroyCancellationToken).Forget());
            if (_deleteButton != null) _deleteButton.onClick.AddListener(() => OnDeleteAsync(destroyCancellationToken).Forget());

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
                    var jsonRepo = new JsonHeroRepository();
                    await jsonRepo.InitializeAsync(ct);
                    _repository = jsonRepo;
                    SetLog("Json リポジトリ(Hero)を初期化しました。");
                }
                else
                {
                    var msgpackRepo = new MessagePackHeroRepository();
                    await msgpackRepo.InitializeAsync(ct);
                    _repository = msgpackRepo;
                    SetLog("MessagePack リポジトリ(Hero)を初期化しました。");
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
            if (!TryParseInput(out var name, out var level, out var position))
            {
                return;
            }

            try
            {
                var hero = new Hero(name, level, position);
                await _txManager.BeginRWTransactionAsync(tx =>
                {
                    _repository.Create(tx, hero);
                }, ct);
                SetLog($"作成: Name={name}, Level={level}, Position={position}");
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
            try
            {
                Hero hero = null;
                _txManager.BeginROTransaction(tx =>
                {
                    // シングルトンなのでID不要
                    hero = _repository.Read(tx);
                });

                if (hero != null)
                {
                    if (_nameInput != null) _nameInput.text = hero.Name;
                    if (_levelInput != null) _levelInput.text = hero.Level.ToString();
                    if (_posXInput != null) _posXInput.text = hero.Position.X.ToString();
                    if (_posYInput != null) _posYInput.text = hero.Position.Y.ToString();
                    SetLog($"読取: Name={hero.Name}, Level={hero.Level}, Position={hero.Position}");
                }
                else
                {
                    SetLog($"Heroデータは存在しません。");
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
            if (!TryParseInput(out var name, out var level, out var position))
            {
                return;
            }

            try
            {
                var hero = new Hero(name, level, position);
                await _txManager.BeginRWTransactionAsync(tx =>
                {
                    // UpdateもID不要でそのまま渡す
                    _repository.Update(tx, hero);
                }, ct);
                SetLog($"更新: Name={name}, Level={level}, Position={position}");
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
            try
            {
                await _txManager.BeginRWTransactionAsync(tx =>
                {
                    // DeleteもID不要
                    _repository.Delete(tx);
                }, ct);
                SetLog($"削除: Heroデータを削除しました。");
                RefreshList();
            }
            catch (Exception ex)
            {
                SetLog($"削除エラー: {ex.Message}");
            }
        }

        /// <summary>
        /// 現在の状態を更新する。
        /// </summary>
        private void RefreshList()
        {
            if (_listHeaderText == null || _listText == null) return;

            try
            {
                Hero hero = null;
                _txManager.BeginROTransaction(tx =>
                {
                    hero = _repository.Read(tx);
                });

                if (hero == null)
                {
                    _listHeaderText.text = "現在のデータ (未作成)";
                    _listText.text = "データなし";
                    return;
                }

                _listHeaderText.text = "現在のデータ (1件)";
                _listText.text = $"Name: {hero.Name}\nLevel: {hero.Level}\nPos: {hero.Position}";
            }
            catch (Exception ex)
            {
                _listText.text = $"データ取得エラー: {ex.Message}";
            }
        }

        /// <summary>
        /// 入力フィールドをパースする。
        /// </summary>
        private bool TryParseInput(out string name, out int level, out Position position)
        {
            name = string.Empty;
            level = 0;
            position = default;

            if (_nameInput != null)
            {
                name = _nameInput.text;
                if (string.IsNullOrEmpty(name))
                {
                    SetLog("名前を入力してください。");
                    return false;
                }
            }

            if (_levelInput != null)
            {
                if (!int.TryParse(_levelInput.text, out level))
                {
                    SetLog("レベルを正しく入力してください。");
                    return false;
                }
            }

            if (_posXInput != null && _posYInput != null)
            {
                if (!int.TryParse(_posXInput.text, out var posX) || !int.TryParse(_posYInput.text, out var posY))
                {
                    SetLog("座標を正しく入力してください。");
                    return false;
                }
                position = new Position(posX, posY);
            }

            return true;
        }

        /// <summary>
        /// ログを設定する。
        /// </summary>
        private void SetLog(string message)
        {
            Debug.Log($"[HeroRepositoryTest] {message}");
            if (_logText != null)
            {
                _logText.text = message;
            }
        }
    }
}
