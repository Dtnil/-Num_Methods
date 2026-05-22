namespace InterpolationApp
{
    /// <summary>
    /// Lagrange interpolation polynomial.
    /// P(x) = sum_{i=0}^{n} y_i * l_i(x)
    /// where l_i(x) = prod_{j≠i} (x - x_j)/(x_i - x_j)
    /// </summary>
    public class LagrangeInterpolation
    {
        private readonly double[] _xs;
        private readonly double[] _ys;

        public LagrangeInterpolation(double[] xs, double[] ys)
        {
            _xs = (double[])xs.Clone();
            _ys = (double[])ys.Clone();
        }

        public double Evaluate(double x)
        {
            int n = _xs.Length;
            double result = 0.0;

            for (int i = 0; i < n; i++)
            {
                double li = 1.0;
                for (int j = 0; j < n; j++)
                {
                    if (j == i) continue;
                    li *= (x - _xs[j]) / (_xs[i] - _xs[j]);
                }
                result += _ys[i] * li;
            }

            return result;
        }
    }
}
