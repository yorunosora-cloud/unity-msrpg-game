using UnityEngine.InputSystem;

/// <summary>
/// 키보드 입력 테스트의 공통 베이스. Input System은 기본적으로 앱/게임 뷰가 포커스를 잃으면 키보드 장치를
/// 비활성화한다(backgroundBehavior, 에디터의 editorInputBehaviorInPlayMode). 배치 모드·다른 창 조작으로
/// 포커스 상태가 흔들리면 테스트의 가상 키 입력이 가끔 무시되어(전체 실행에서만 간헐 실패) 이를 고정한다.
/// </summary>
public abstract class InputFixtureBase : InputTestFixture
{
    public override void Setup()
    {
        base.Setup();
        InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
#if UNITY_EDITOR
        InputSystem.settings.editorInputBehaviorInPlayMode =
            InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
#endif
    }
}
