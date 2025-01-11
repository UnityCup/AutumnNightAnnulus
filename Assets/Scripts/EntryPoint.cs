using System;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using Jint;
using LitMotion;
using LitMotion.Extensions;
using Lua;
using Lua.Standard;
using TMPro;
using TypeScriptImporter;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace LuaAdventure
{
    public class EntryPoint : MonoBehaviour
    {
        [SerializeField] private TMP_Text _tmpText;
        [SerializeField] private TMP_Text _choice0Text;
        [SerializeField] private TMP_Text _choice1Text;
        [SerializeField] private float _textDuration;
        [SerializeField] private Transform _nextTextCursor;
        [SerializeField] private RectTransform _cursorTransform;
        [SerializeField] private TMP_Text _gameoverText;
        [SerializeField] private Ease _gameoverTextAlphaEase;
        [SerializeField] private float _gameoverTextAlphaDuration;
        [SerializeField] private Image _fadeImage;
        [SerializeField] private Ease _fadeEase;
        [SerializeField] private float _fadeDuration;
        [SerializeField] private Vector2 _choice0Position;
        [SerializeField] private Vector2 _choice1Position;
        [SerializeField] private int _choiceIndex;
        [SerializeField] private string _choice0;
        [SerializeField] private string _choice1;
        [SerializeField] private bool _chose;
        [SerializeField] private bool _next;
        [SerializeField] private bool _retry;
        [SerializeField] private double _timerStartTime;
        [SerializeField] private MotionHandle _textMotionHandle;

        private void Start()
        {
            _chose = true;
            _next = true;
            _retry = true;
            LMotion
                .Create(1f, 0f, _fadeDuration)
                .WithEase(_fadeEase)
                .BindToColorA(_fadeImage);
            StartLogic();
        }

        private void Update()
        {
            _cursorTransform.gameObject.SetActive(!_chose);
            _cursorTransform.anchoredPosition = _choiceIndex == 0 ? _choice0Position : _choice1Position;
            _nextTextCursor.gameObject.SetActive(!_next);
            _choice0Text.gameObject.SetActive(!_chose);
            _choice1Text.gameObject.SetActive(!_chose);

            if (!_next)
                if (Input.GetKeyDown(KeyCode.Space))
                {
                    if (_tmpText.maxVisibleCharacters < _tmpText.text.Length && _textMotionHandle.IsActive())
                        _textMotionHandle.Complete();
                    else
                        _next = true;
                }

            if (!_chose)
            {
                if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.DownArrow) ||
                    Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.S))
                    _choiceIndex = (_choiceIndex + 1) % 2;

                if (Input.GetKeyDown(KeyCode.Space)) _chose = true;
            }

            if (!_retry)
                if (Input.GetKeyDown(KeyCode.Space))
                    _retry = true;
        }

        private void StartLogic()
        {
            TsScript();
            LuaScript();
        }

        private void TsExample()
        {
            var engine = new Engine()
                .SetValue("log", new Action<object>(Debug.Log));
            var tsExample = Resources.Load<TypeScriptAsset>("TypeScript/example");
            engine.Execute(tsExample.JavaScriptSource);
        }

        private void TsScript()
        {
            TsExample();
            var engine = new Engine();
        }

        private void LuaScript()
        {
            var luaState = LuaState.Create();
            var textAsset = Resources.Load("LuaScript", typeof(TextAsset)) as TextAsset;
            var code = textAsset.text;

            luaState.Environment["wait"] = new LuaFunction(async (context, memory, cancellationToken) =>
            {
                var arg = context.GetArgument<double>(0);
                await ScriptWait(arg, cancellationToken);
                return 0;
            });

            luaState.Environment["debuglog"] = new LuaFunction((context, memory, cancellationToken) =>
            {
                var arg = context.GetArgument<string>(0);
                ScriptDebugLog(arg);
                return new ValueTask<int>(0);
            });

            luaState.Environment["annulus"] = new LuaFunction(async (context, memory, cancellationToken) =>
            {
                var text = context.GetArgument<string>(0);
                await ScriptAnnulus(text, cancellationToken);
                return 0;
            });

            luaState.Environment["settext"] = new LuaFunction(async (context, memory, cancellationToken) =>
            {
                var text = context.GetArgument<string>(0);
                await ScriptSetText(text, cancellationToken);
                return 0;
            });

            luaState.Environment["choice"] = new LuaFunction(async (context, memory, cancellationToken) =>
            {
                var choice0 = context.GetArgument<string>(0);
                var choice1 = context.GetArgument<string>(1);

                var result = await ScriptChoice(choice0, choice1, cancellationToken);

                memory.Span[0] = result;

                return 1;
            });

            luaState.Environment["gameover"] = new LuaFunction(async (context, memory, cancellationToken) =>
            {
                await ScriptGameOver(cancellationToken);

                return 0;
            });

            luaState.Environment["starttimer"] = new LuaFunction((context, memory, cancellationToken) =>
            {
                ScriptStartTimer(cancellationToken);
                return new ValueTask<int>(0);
            });

            luaState.Environment["endtimer"] = new LuaFunction((context, memory, cancellationToken) =>
            {
                var time = ScriptEndTimer(cancellationToken);
                memory.Span[0] = time;
                return new ValueTask<int>(1);
            });

            luaState.DoStringAsync(code, cancellationToken: destroyCancellationToken);
            luaState.OpenStandardLibraries();
        }

        private async UniTask SetText(string text, CancellationToken cancellationToken)
        {
            _tmpText.text = text;
            if (_textMotionHandle.IsActive()) _textMotionHandle.Cancel();
            _tmpText.maxVisibleCharacters = 0;
            _textMotionHandle = LMotion
                .Create(0f, _tmpText.text.Length, _textDuration * text.Length)
                .Bind(f => _tmpText.maxVisibleCharacters = (int)f);
            _next = false;

            await UniTask.WaitWhile(this, e => !e._next, cancellationToken: cancellationToken);
        }

        private async UniTask ScriptWait(double value, CancellationToken cancellationToken = default)
        {
            await UniTask.Delay(TimeSpan.FromSeconds(value), cancellationToken: cancellationToken);
        }

        private void ScriptDebugLog(string value)
        {
            Debug.Log(value);
        }

        private async UniTask ScriptAnnulus(string text, CancellationToken cancellationToken = default)
        {
            await SetText($"「{text}」", cancellationToken);
        }

        private async UniTask ScriptSetText(string text, CancellationToken cancellationToken = default)
        {
            await SetText($"＊ {text}".Replace("\n", "\n"), cancellationToken);
        }

        private async UniTask<bool> ScriptChoice(string choice0, string choice1,
            CancellationToken cancellationToken = default)
        {
            _choice0Text.text = choice0;
            _choice1Text.text = choice1;

            _chose = false;

            await UniTask.WaitWhile(this, e => !e._chose, cancellationToken: cancellationToken = default);

            return _choiceIndex == 0;
        }

        private async UniTask ScriptGameOver(CancellationToken cancellationToken = default)
        {
            _chose = true;
            _next = true;

            LMotion
                .Create(0f, 1f, _gameoverTextAlphaDuration)
                .WithEase(_gameoverTextAlphaEase)
                .BindToColorA(_gameoverText);

            await UniTask.Delay(TimeSpan.FromSeconds(_gameoverTextAlphaDuration),
                cancellationToken: cancellationToken);

            _retry = false;
            await UniTask.WaitWhile(this, e => !e._retry, cancellationToken: cancellationToken);

            SceneManager.LoadScene("SampleScene");
        }

        private void ScriptStartTimer(CancellationToken cancellationToken = default)
        {
            _timerStartTime = Time.timeAsDouble;
        }

        private int ScriptEndTimer(CancellationToken cancellationToken = default)
        {
            var time = Time.timeAsDouble - _timerStartTime;
            return Mathf.CeilToInt((float)time);
        }
    }
}
