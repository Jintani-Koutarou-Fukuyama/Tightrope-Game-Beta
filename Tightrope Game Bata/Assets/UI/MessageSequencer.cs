using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class MessageSequencer : MonoBehaviour
{
    [SerializeField]
    private TMP_Text _textUi = default;

    [SerializeField]
    private string[] _messages = default;

    
    private int _currentIndex = -1;//メッセージの切り替え、-1は何も映さない

    private void Start()
    {
        MoveNext();
    }

    private void Update()
    {
        /*
        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            MoveNext();
        }
        */
    }

    /// <summary>
    /// 次のページに進む。
    /// 次のページが存在しない場合は無視する。
    /// </summary>
    public void MoveNext()
    {
        if (_messages is null or { Length: 0 }) { return; }

        if (_currentIndex + 1 < _messages.Length)
        {
            _currentIndex++;
            ShowMessage(_messages[_currentIndex]);
        }
    }

    /// <summary>
    /// 指定のメッセージを表示する。
    /// </summary>
    /// <param name="message">テキストとして表示するメッセージ。</param>
    private void ShowMessage(string message)
    {
        if (_textUi == null) { return; }
        _textUi.text = message;
    }
}