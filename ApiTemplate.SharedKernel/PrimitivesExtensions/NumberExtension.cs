using System;

namespace ApiTemplate.SharedKernel.PrimitivesExtensions
{
    public static class NumberExtension
    {
        public static decimal ToPercentFixed2(this decimal n)
        {
            return Math.Round(n / 100, 2);
        }

        public static double ToPercentFixed2(this double n)
        {
            return Math.Round(n / 100, 2);
        }

        public static double FromPercentFixed2(this double n)
        {
            return Math.Round(n * 100, 2);
        }

        public static double ToFixed2(this double n)
        {
            return Math.Round(n, 2);
        }

        public static decimal Round(this decimal amount, int? precision = 0)
               => Math.Round(amount, precision.Value, MidpointRounding.ToPositiveInfinity);

        public static double PMT(double yearlyInterestRate, int totalNumberOfMonths, double loanAmount)
        {
            var rate = (double)yearlyInterestRate / 100 / 12;
            var denominator = Math.Pow((1 + rate), totalNumberOfMonths) - 1;
            return (rate + (rate / denominator)) * loanAmount;
        }
    }
}
