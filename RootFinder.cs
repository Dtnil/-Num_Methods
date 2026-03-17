using System;
using System.Collections.Generic;

public class RootFinder
{
    double eps = 0.01;

    public double Der_point(double a, int eq)
    {
        return (Equations.F(eq, a + 0.005) - Equations.F(eq, a)) / 0.005;

    }
    public List<double> FindRoots(int eq, double a, double b, int rec_d =0)
    {
        List<double> roots = new List<double>();

        double step = Math.Abs(b - a) / 10.0;

        for (int i = 0; i < 10; i++)
        {

            double c = a + step * i;
            double minstep = Math.Abs(c+step - c) / 5.0;

            bool der = false;
            bool der_sin = Der_point(c, eq) > 0;

            for (int j = 1; j < 5; j++)
            {
                if (der_sin != Der_point(c + minstep*j, eq) > 0)
                {
                    der = true;
                    break;
                }
            }
            //Console.Write($"{new String(' ', rec_d)}{c} {c+step} {der}   {step}\n");
            if (der && step > eps)
            {
                foreach(var rec_res in FindRoots(eq, c, c + step, rec_d+1))
                    roots.Add(rec_res);
                
            }
            else if (Equations.F(eq, c) * Equations.F(eq, c + step) < 0)
            {
                double root = Chord(eq, c, c + step);
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
