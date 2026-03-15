using System;
using System.Collections.Generic;

public class RootFinder
{
    double eps = 0.01;

    public List<double> FindRoots(int eq, double a, double b)
    {
        List<double> roots = new List<double>();

        double step = (b - a) / 10000.0;

        for (double x = a; x < b; x += step)
        {
            double x1 = x;
            double x2 = x + step;

            double f1 = Equations.F(eq, x1);
            double f2 = Equations.F(eq, x2);

            if (f1 * f2 <= 0)
            {
                double root = Chord(eq, x1, x2);

                bool exists = false;

                foreach (double r in roots)
                {
                    if (Math.Abs(r - root) < eps)
                    {
                        exists = true;
                        break;
                    }
                }

                if (!exists)
                    roots.Add(root);
            }
        }

        return roots;
    }

    double Chord(int eq, double a, double b)
    {
        double x;

        do
        {
            x = a - Equations.F(eq, a) * (b - a) /
                (Equations.F(eq, b) - Equations.F(eq, a));

            if (Equations.F(eq, a) * Equations.F(eq, x) < 0)
                b = x;
            else
                a = x;

        } while (Math.Abs(Equations.F(eq, x)) > eps);

        return x;
    }
}