using UnityEngine;
using UnityEngine.InputSystem;

public struct FarmInputFrame
{
    public Vector2 Move, WheelDirection, CameraStick;
    public Vector2 PointerPosition;
    public bool PointerAvailable;
    public Vector2Int Navigate;
    public bool Confirm, Cancel, Wheel, Inventory, Sleep, Shop, Build, Orbit, Pause, Rotate, Delete;
    public int Tab, Quantity, DirectTool;
}

public sealed class FarmInput
{
    public bool UsingGamepad { get; private set; }
    public bool UsingMouse { get; private set; }=true;
    public void MarkMouseUsed(){UsingGamepad=false;UsingMouse=true;}
    public bool HasGamepad
    {
        get
        {
#if ENABLE_INPUT_SYSTEM
            if(Gamepad.current!=null)return true;
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
            if(Application.isPlaying)foreach(string name in Input.GetJoystickNames())
                if(!string.IsNullOrEmpty(name))return true;
#endif
            return false;
        }
    }
    float nextRepeat;
    Vector2Int previousDirection;
    Vector2 lastMouse;
    bool mouseReady, legacyLeftTrigger, legacyRightTrigger;

    // Unity peut continuer à utiliser Input Manager tant que l'éditeur n'a pas
    // redémarré après le changement d'Active Input Handling. Respecter les
    // symboles réellement compilés, pas seulement le fichier de réglages.
    public FarmInputFrame Read(float time)
    {
        Keyboard k=null;Gamepad p=null;Mouse m=null;
#if ENABLE_INPUT_SYSTEM
        k=Keyboard.current;p=Gamepad.current;m=Mouse.current;
#endif
        var f=new FarmInputFrame { DirectTool=-1 };
        if(k!=null&&k.enabled)ReadKeyboard(k,ref f);
#if ENABLE_LEGACY_INPUT_MANAGER
        else if(Application.isPlaying)ReadLegacyKeyboard(ref f);
#endif
        Vector2 nav=f.Move;
        if(p!=null&&p.enabled)ReadGamepad(p,ref f,ref nav);
#if ENABLE_LEGACY_INPUT_MANAGER
        else if(Application.isPlaying)ReadLegacyGamepad(ref f,ref nav);
#endif
        if(m!=null&&m.enabled)
        {
            Vector2 pos=m.position.ReadValue();
            f.PointerPosition=pos;f.PointerAvailable=true;
            if((mouseReady&&(pos-lastMouse).sqrMagnitude>4)||m.leftButton.wasPressedThisFrame||m.rightButton.wasPressedThisFrame||m.middleButton.isPressed)MarkMouseUsed();
            if(m.middleButton.isPressed){f.Orbit=true;f.CameraStick.x+=m.delta.ReadValue().x*.15f;}
            lastMouse=pos;mouseReady=true;
        }
#if ENABLE_LEGACY_INPUT_MANAGER
        else if(Application.isPlaying)
        {
            Vector2 pos=Input.mousePosition;f.PointerPosition=pos;f.PointerAvailable=true;
            if((mouseReady&&(pos-lastMouse).sqrMagnitude>4)||Input.GetMouseButtonDown(0)||Input.GetMouseButtonDown(1)||Input.GetMouseButton(2))MarkMouseUsed();
            if(Input.GetMouseButton(2)&&mouseReady){f.Orbit=true;f.CameraStick.x+=(pos.x-lastMouse.x)*.15f;}
            lastMouse=pos;mouseReady=true;
        }
#endif
        var direction=new Vector2Int(Mathf.Abs(nav.x)>.55f?(nav.x>0?1:-1):0,Mathf.Abs(nav.y)>.55f?(nav.y>0?1:-1):0);
        if(direction==Vector2Int.zero){previousDirection=direction;nextRepeat=0;}
        else if(direction!=previousDirection||time>=nextRepeat)
        {
            f.Navigate=direction;
            nextRepeat=time+(direction!=previousDirection?.32f:.16f);
            previousDirection=direction;
        }
        f.Move=Vector2.ClampMagnitude(f.Move,1);
        return f;
    }
    void ReadKeyboard(Keyboard k,ref FarmInputFrame f)
    {
        f.Move=new Vector2((Down(k,Key.D)||Down(k,Key.RightArrow)?1:0)-(Down(k,Key.A)||Down(k,Key.Q)||Down(k,Key.LeftArrow)?1:0),
            (Down(k,Key.W)||Down(k,Key.Z)||Down(k,Key.UpArrow)?1:0)-(Down(k,Key.S)||Down(k,Key.DownArrow)?1:0));
        f.Confirm=Pressed(k,Key.E)||Pressed(k,Key.Space)||Pressed(k,Key.Enter);
        f.Cancel=Pressed(k,Key.Escape);f.Wheel=Pressed(k,Key.Tab);f.Inventory=Pressed(k,Key.I);
        f.Sleep=Pressed(k,Key.N);f.Shop=Pressed(k,Key.B);f.Build=Pressed(k,Key.G);f.Orbit=Down(k,Key.LeftAlt);
        f.Rotate=Pressed(k,Key.R);f.Delete=Pressed(k,Key.Delete);
        f.CameraStick=new Vector2((Down(k,Key.C)?1:0)-(Down(k,Key.V)?1:0),0);
        f.Tab=(Pressed(k,Key.PageDown)?1:0)-(Pressed(k,Key.PageUp)?1:0);
        f.Quantity=(Pressed(k,Key.Equals)||Pressed(k,Key.NumpadPlus)?1:0)-(Pressed(k,Key.Minus)||Pressed(k,Key.NumpadMinus)?1:0);
        Key[] digits={Key.Digit1,Key.Digit2,Key.Digit3,Key.Digit4,Key.Digit5,Key.Digit6};
        for(int i=0;i<digits.Length;i++)if(Pressed(k,digits[i]))f.DirectTool=i;
        if(KeyboardUsed(f)){UsingGamepad=false;UsingMouse=false;}
    }
    void ReadGamepad(Gamepad p,ref FarmInputFrame f,ref Vector2 nav)
    {
        Vector2 stick=p.leftStick.ReadValue(),look=p.rightStick.ReadValue(),dpad=p.dpad.ReadValue();
        if(stick.sqrMagnitude>.04f)f.Move=stick;
        f.WheelDirection=look.sqrMagnitude>.1f?look:stick;
        f.CameraStick=look;f.Orbit|=p.leftTrigger.isPressed;
        f.Confirm|=p.buttonSouth.wasPressedThisFrame;f.Cancel|=p.buttonEast.wasPressedThisFrame;
        f.Wheel|=p.buttonWest.wasPressedThisFrame;f.Inventory|=p.buttonNorth.wasPressedThisFrame;
        f.Delete|=p.rightStickButton.wasPressedThisFrame;
        f.Pause|=p.startButton.wasPressedThisFrame;f.Shop|=p.rightShoulder.wasPressedThisFrame;
        f.Tab+=(p.rightShoulder.wasPressedThisFrame?1:0)-(p.leftShoulder.wasPressedThisFrame?1:0);
        f.Quantity+=(p.rightTrigger.wasPressedThisFrame?1:0)-(p.leftTrigger.wasPressedThisFrame?1:0);
        if(dpad.sqrMagnitude>.1f)nav=dpad;else if(stick.sqrMagnitude>.1f)nav=stick;
        if(stick.sqrMagnitude>.06f||look.sqrMagnitude>.06f||dpad.sqrMagnitude>.1f||
            p.buttonSouth.wasPressedThisFrame||p.buttonEast.wasPressedThisFrame||p.buttonWest.wasPressedThisFrame||
            p.buttonNorth.wasPressedThisFrame||p.startButton.wasPressedThisFrame||p.leftShoulder.wasPressedThisFrame||
            p.rightShoulder.wasPressedThisFrame||p.leftTrigger.wasPressedThisFrame||p.rightTrigger.wasPressedThisFrame){UsingGamepad=true;UsingMouse=false;}
    }
#if ENABLE_LEGACY_INPUT_MANAGER
    void ReadLegacyKeyboard(ref FarmInputFrame f)
    {
        f.Move=new Vector2((Held(KeyCode.D)||Held(KeyCode.RightArrow)?1:0)-(Held(KeyCode.A)||Held(KeyCode.Q)||Held(KeyCode.LeftArrow)?1:0),
            (Held(KeyCode.W)||Held(KeyCode.Z)||Held(KeyCode.UpArrow)?1:0)-(Held(KeyCode.S)||Held(KeyCode.DownArrow)?1:0));
        f.Confirm=Hit(KeyCode.E)||Hit(KeyCode.Space)||Hit(KeyCode.Return)||Hit(KeyCode.KeypadEnter);
        f.Cancel=Hit(KeyCode.Escape);f.Wheel=Hit(KeyCode.Tab);f.Inventory=Hit(KeyCode.I);
        f.Sleep=Hit(KeyCode.N);f.Shop=Hit(KeyCode.B);f.Build=Hit(KeyCode.G);f.Orbit=Held(KeyCode.LeftAlt);
        f.Rotate=Hit(KeyCode.R);f.Delete=Hit(KeyCode.Delete);
        f.CameraStick=new Vector2((Held(KeyCode.C)?1:0)-(Held(KeyCode.V)?1:0),0);
        f.Tab=(Hit(KeyCode.PageDown)?1:0)-(Hit(KeyCode.PageUp)?1:0);
        f.Quantity=(Hit(KeyCode.Equals)||Hit(KeyCode.KeypadPlus)?1:0)-(Hit(KeyCode.Minus)||Hit(KeyCode.KeypadMinus)?1:0);
        for(int i=0;i<6;i++)if(Hit((KeyCode)((int)KeyCode.Alpha1+i)))f.DirectTool=i;
        if(KeyboardUsed(f)){UsingGamepad=false;UsingMouse=false;}
    }
    void ReadLegacyGamepad(ref FarmInputFrame f,ref Vector2 nav)
    {
        bool connected=false;
        foreach(string name in Input.GetJoystickNames())if(!string.IsNullOrEmpty(name)){connected=true;break;}
        if(!connected){legacyLeftTrigger=legacyRightTrigger=false;return;}
        Vector2 stick=new Vector2(Input.GetAxisRaw("FarmMoveX"),Input.GetAxisRaw("FarmMoveY"));
        Vector2 look=new Vector2(Input.GetAxisRaw("FarmLookX"),Input.GetAxisRaw("FarmLookY"));
        Vector2 dpad=new Vector2(Input.GetAxisRaw("FarmDpadX"),Input.GetAxisRaw("FarmDpadY"));
        if(stick.sqrMagnitude>.04f)f.Move=stick;
        f.WheelDirection=look.sqrMagnitude>.1f?look:stick;f.CameraStick=look;
        bool a=Hit(KeyCode.JoystickButton0),b=Hit(KeyCode.JoystickButton1);
        bool x=Hit(KeyCode.JoystickButton2),y=Hit(KeyCode.JoystickButton3);
        bool lb=Hit(KeyCode.JoystickButton4),rb=Hit(KeyCode.JoystickButton5),start=Hit(KeyCode.JoystickButton7);
        f.Confirm|=a;f.Cancel|=b;f.Wheel|=x;f.Inventory|=y;f.Pause|=start;f.Shop|=rb;f.Tab+=(rb?1:0)-(lb?1:0);
        f.Delete|=Hit(KeyCode.JoystickButton9);
        float triggers=Input.GetAxisRaw("FarmTriggers");
        bool lt=triggers>.5f,rt=triggers<-.5f;
        f.Orbit|=lt;f.Quantity+=(rt&&!legacyRightTrigger?1:0)-(lt&&!legacyLeftTrigger?1:0);
        legacyLeftTrigger=lt;legacyRightTrigger=rt;
        if(dpad.sqrMagnitude>.1f)nav=dpad;else if(stick.sqrMagnitude>.1f)nav=stick;
        if(stick.sqrMagnitude>.06f||look.sqrMagnitude>.06f||dpad.sqrMagnitude>.1f||a||b||x||y||lb||rb||start||lt||rt){UsingGamepad=true;UsingMouse=false;}
    }
    static bool Held(KeyCode key)=>Input.GetKey(key);
    static bool Hit(KeyCode key)=>Input.GetKeyDown(key);
#endif
    static bool KeyboardUsed(FarmInputFrame f)=>f.Move.sqrMagnitude>0||f.Confirm||f.Cancel||f.Wheel||f.Inventory||f.Sleep||f.Shop||f.Build||f.Pause||f.Rotate||f.Delete||f.Orbit||f.Tab!=0||f.Quantity!=0||f.DirectTool>=0;
    static bool Down(Keyboard k,Key key)=>k[key].isPressed;
    static bool Pressed(Keyboard k,Key key)=>k[key].wasPressedThisFrame;
}

public static class FarmToolWheel
{
    public static int Select(Vector2 direction,int previous)
    {
        if(direction.sqrMagnitude<.16f)return previous;
        float angle=Mathf.Atan2(direction.x,direction.y)*Mathf.Rad2Deg;
        return (Mathf.RoundToInt(angle/60)+6)%6;
    }
}
