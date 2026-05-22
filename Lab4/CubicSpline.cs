using System;

namespace InterpolationApp
{
    /// <summary>
    /// Cubic Spline interpolation.
    /// Supports:
    ///   - Clamped (clamped / зімкнений): known first derivatives at endpoints S'(x0) and S'(xN)
    ///   - Natural (природній): S''(x0)=0, S''(xN)=0
    ///
    /// Each piecewise polynomial is stored as coefficients [s3, s2, s1, s0]
    /// evaluated via Horner's method: Sk(x) = s3*ω³ + s2*ω² + s1*ω + s0, ω = x - xk
    /// </summary>
    public class CubicSpline
    {
        private readonly double[] _xs;
        private readonly double[] _s0, _s1, _s2, _s3; // spline coefficients per segment

        private CubicSpline(double[] xs, double[] s0, double[] s1, double[] s2, double[] s3)
        {
            _xs = xs;
            _s0 = s0; _s1 = s1; _s2 = s2; _s3 = s3;
        }

        // ---------- Factory methods ----------

        public static CubicSpline Clamped(double[] xs, double[] ys, double dydx0, double dydxN)
        {
            int n = xs.Length;
            int N = n - 1; // number of intervals

            double[] h = new double[N];
            double[] d = new double[N];
            for (int k = 0; k < N; k++)
            {
                h[k] = xs[k + 1] - xs[k];
                d[k] = (ys[k + 1] - ys[k]) / h[k];
            }

            // Build tridiagonal system for m[1]..m[N-1]
            // First row (k=1) uses clamped BC at x0
            // Last row (k=N-1) uses clamped BC at xN
            // m0 = 3/h0*(d0 - S'(x0)) - m1/2
            // mN = 3/hN-1*(S'(xN) - dN-1) - mN-1/2

            int sz = N - 1; // unknowns m1..mN-1
            if (sz <= 0)
            {
                // Only 2 points -> linear; treat as natural
                return Natural(xs, ys);
            }

            double[] lower = new double[sz];
            double[] diag  = new double[sz];
            double[] upper = new double[sz];
            double[] rhs   = new double[sz];

            // k = 1 (first equation):
            // (3/2*h0 + 2h1)*m1 + h1*m2 = u1 - 3*(d0 - dydx0)
            double u1 = 6.0 * (d[1] - d[0]);
            diag[0]  = 1.5 * h[0] + 2.0 * h[1];
            upper[0] = (sz > 1) ? h[1] : 0;
            rhs[0]   = u1 - 3.0 * (d[0] - dydx0);

            for (int i = 1; i < sz - 1; i++)
            {
                int k = i + 1;
                double uk = 6.0 * (d[k] - d[k - 1]);
                lower[i] = h[k - 1];
                diag[i]  = 2.0 * (h[k - 1] + h[k]);
                upper[i] = h[k];
                rhs[i]   = uk;
            }

            if (sz > 1)
            {
                // k = N-1 (last equation):
                // hN-2*mN-2 + (2hN-2 + 3/2*hN-1)*mN-1 = uN-1 - 3*(dydxN - dN-1)
                double uNm1 = 6.0 * (d[N - 1] - d[N - 2]);
                lower[sz - 1] = h[N - 2];
                diag[sz - 1]  = 2.0 * h[N - 2] + 1.5 * h[N - 1];
                rhs[sz - 1]   = uNm1 - 3.0 * (dydxN - d[N - 1]);
            }

            double[] mInner = SolveTridiagonal(lower, diag, upper, rhs);

            // Assemble full m array [m0, m1, ..., mN]
            double[] m = new double[n];
            for (int i = 0; i < sz; i++) m[i + 1] = mInner[i];

            // Boundary moments from clamped conditions
            m[0] = 3.0 / h[0] * (d[0] - dydx0) - m[1] / 2.0;
            m[N] = 3.0 / h[N - 1] * (dydxN - d[N - 1]) - m[N - 1] / 2.0;

            return BuildCoefficients(xs, ys, h, d, m, N);
        }

        public static CubicSpline Natural(double[] xs, double[] ys)
        {
            int n = xs.Length;
            int N = n - 1;

            double[] h = new double[N];
            double[] d = new double[N];
            for (int k = 0; k < N; k++)
            {
                h[k] = xs[k + 1] - xs[k];
                d[k] = (ys[k + 1] - ys[k]) / h[k];
            }

            // m0 = mN = 0, solve for m1..mN-1
            int sz = N - 1;
            if (sz <= 0)
            {
                // Linear spline
                double[] s0L = new[] { ys[0] };
                double[] s1L = new[] { d[0] };
                double[] s2L = new[] { 0.0 };
                double[] s3L = new[] { 0.0 };
                return new CubicSpline(xs, s0L, s1L, s2L, s3L);
            }

            double[] lower = new double[sz];
            double[] diag  = new double[sz];
            double[] upper = new double[sz];
            double[] rhs   = new double[sz];

            for (int i = 0; i < sz; i++)
            {
                int k = i + 1;
                double uk = 6.0 * (d[k] - d[k - 1]);
                lower[i] = (i > 0) ? h[k - 1] : 0;
                diag[i]  = 2.0 * (h[k - 1] + h[k]);
                upper[i] = (i < sz - 1) ? h[k] : 0;
                rhs[i]   = uk;
            }
            // Adjust for m0=0 and mN=0 (already 0 so no adjustment needed)

            double[] mInner = SolveTridiagonal(lower, diag, upper, rhs);

            double[] m = new double[n];
            for (int i = 0; i < sz; i++) m[i + 1] = mInner[i];
            m[0] = 0; m[N] = 0;

            return BuildCoefficients(xs, ys, h, d, m, N);
        }

        // ---------- Core helpers ----------

        private static CubicSpline BuildCoefficients(double[] xs, double[] ys, double[] h, double[] d, double[] m, int N)
        {
            double[] s0 = new double[N];
            double[] s1 = new double[N];
            double[] s2 = new double[N];
            double[] s3 = new double[N];

            for (int k = 0; k < N; k++)
            {
                s0[k] = ys[k];
                s1[k] = d[k] - h[k] * (2.0 * m[k] + m[k + 1]) / 6.0;
                s2[k] = m[k] / 2.0;
                s3[k] = (m[k + 1] - m[k]) / (6.0 * h[k]);
            }

            return new CubicSpline(xs, s0, s1, s2, s3);
        }

        /// <summary>Thomas algorithm for tridiagonal system.</summary>
        private static double[] SolveTridiagonal(double[] lower, double[] diag, double[] upper, double[] rhs)
        {
            int n = rhs.Length;
            double[] c = new double[n];
            double[] d = new double[n];
            double[] x = new double[n];

            c[0] = upper[0] / diag[0];
            d[0] = rhs[0] / diag[0];

            for (int i = 1; i < n; i++)
            {
                double denom = diag[i] - lower[i] * c[i - 1];
                c[i] = (i < n - 1) ? upper[i] / denom : 0;
                d[i] = (rhs[i] - lower[i] * d[i - 1]) / denom;
            }

            x[n - 1] = d[n - 1];
            for (int i = n - 2; i >= 0; i--)
                x[i] = d[i] - c[i] * x[i + 1];

            return x;
        }

        // ---------- Evaluation ----------

        public double Evaluate(double x)
        {
            int n = _xs.Length;
            int k = n - 2; // default to last segment

            for (int i = 0; i < n - 1; i++)
            {
                if (x <= _xs[i + 1])
                {
                    k = i;
                    break;
                }
            }

            double omega = x - _xs[k];
            // Horner: ((s3*ω + s2)*ω + s1)*ω + s0
            return ((_s3[k] * omega + _s2[k]) * omega + _s1[k]) * omega + _s0[k];
        }
    }
}
