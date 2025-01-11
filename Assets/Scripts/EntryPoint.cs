using System;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using LitMotion;
using LitMotion.Extensions;
using Lua;
using Lua.Standard;
using TMPro;
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
        private LuaState _luaState;
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
            _luaState = LuaState.Create();
            _luaState.OpenStandardLibraries();

            _luaState.Environment["wait"] = new LuaFunction(async (context, memory, cancellationToken) =>
            {
                var arg = context.GetArgument<double>(0);
                await UniTask.Delay(TimeSpan.FromSeconds(arg), cancellationToken: cancellationToken);
                return 0;
            });

            _luaState.Environment["debuglog"] = new LuaFunction((context, memory, cancellationToken) =>
            {
                var arg = context.GetArgument<string>(0);
                Debug.Log(arg);
                return new ValueTask<int>(0);
            });

            _luaState.Environment["annulus"] = new LuaFunction(async (context, memory, cancellationToken) =>
            {
                var text = context.GetArgument<string>(0);
                await SetText($"「{text}」", cancellationToken);
                return 0;
            });

            _luaState.Environment["settext"] = new LuaFunction(async (context, memory, cancellationToken) =>
            {
                var text = context.GetArgument<string>(0);
                await SetText($"＊ {text}".Replace("\n", "\n"), cancellationToken);
                return 0;
            });

            _luaState.Environment["choice"] = new LuaFunction(async (context, memory, cancellationToken) =>
            {
                var choice0 = context.GetArgument<string>(0);
                var choice1 = context.GetArgument<string>(1);

                _choice0Text.text = choice0;
                _choice1Text.text = choice1;

                _chose = false;

                await UniTask.WaitWhile(this, e => !e._chose, cancellationToken: cancellationToken);

                memory.Span[0] = _choiceIndex == 0;

                return 1;
            });

            _luaState.Environment["gameover"] = new LuaFunction(async (context, memory, cancellationToken) =>
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

                return 0;
            });

            _luaState.Environment["starttimer"] = new LuaFunction((context, memory, cancellationToken) =>
            {
                _timerStartTime = Time.timeAsDouble;
                return new ValueTask<int>(0);
            });

            _luaState.Environment["endtimer"] = new LuaFunction((context, memory, cancellationToken) =>
            {
                var time = Time.timeAsDouble - _timerStartTime;
                memory.Span[0] = Mathf.CeilToInt((float)time);
                return new ValueTask<int>(1);
            });

            var textAsset = Resources.Load("LuaScript", typeof(TextAsset)) as TextAsset;
            var code = textAsset.text;
            Debug.Log(code);
            _luaState.DoStringAsync(code, cancellationToken: destroyCancellationToken);
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
    }
}
