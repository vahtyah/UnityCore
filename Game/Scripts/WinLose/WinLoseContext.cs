public readonly struct WinLoseContext
{
    public readonly int example;


    public WinLoseContext(int example)
    {
        this.example = example;
    }

    public static WinLoseContext Capture()
    {

        return new WinLoseContext(
            10
        );
    }
}