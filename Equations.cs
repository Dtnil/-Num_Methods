using System;

public static class Equations
{
    public static double F(int id, double x)
    {
        switch (id)
        {
            case 1: return Math.Cos(Math.Sin(Math.Pow(x, 3))) - 0.7;
            case 2: return x * x - 2 * x - 4;
            case 3: return Math.Sin(Math.Pow(x, 3)) / 5.0;
            case 4: return Math.Pow(x, 3) + x * x - 4 * x - 2;
            case 5: return x * x * Math.Cos(x) - 0.2;
            case 6: return 1.5 - Math.Pow(x, 1 - Math.Cos(x));
            case 7: return Math.Pow(x, 3) - 3 * x + 1.5;
            case 8: return Math.Cos(x * x - x + 1);
            case 9: return Math.Exp(x) - Math.Sin(2 * x) - 1;
            case 10: return (Math.Pow(x, 3) - 7 * x + 1) / 3.0;
            case 11: return Math.Cos(x * x) - 0.5;
            case 12: return Math.Sin(x) - 0.5 * Math.Cos(x * x);
            case 13: return Math.Log(Math.Sin(2 * x) + 1);
            case 14: return Math.Sin(x) - Math.Cos(Math.Pow(x, 3));
            default: return 0;
        }
    }
}