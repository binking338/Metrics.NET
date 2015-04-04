﻿using System;
using Metrics.MetricData;
using Metrics.Sampling;

namespace Metrics.Core
{
    public interface HistogramImplementation : Histogram, MetricValueProvider<HistogramValue> { }

    public sealed class HistogramMetric : HistogramImplementation
    {
        private readonly Reservoir reservoir;
        private UserValueWrapper last;

        public HistogramMetric()
            : this(new ExponentiallyDecayingReservoir()) { }

        public HistogramMetric(SamplingType samplingType)
            : this(SamplingTypeToReservoir(samplingType)) { }

        public HistogramMetric(Reservoir reservoir)
        {
            this.reservoir = reservoir;
        }

        public void Update(long value, string userValue = null)
        {
            this.last = new UserValueWrapper(value, userValue);
            this.reservoir.Update(value, userValue);
        }

        public bool Merge(MetricValueProvider<HistogramValue> other)
        {
            var hOther = other as HistogramMetric;
            if (hOther != null)
            {
                return Merge(hOther);
            }
            return false;
        }

        public HistogramValue GetValue(bool resetMetric = false)
        {
            var value = new HistogramValue(this.last.Value, this.last.UserValue, this.reservoir.GetSnapshot(resetMetric));
            if (resetMetric)
            {
                this.last = UserValueWrapper.Empty;
            }
            return value;
        }

        public HistogramValue Value
        {
            get
            {
                return GetValue();
            }
        }

        public void Reset()
        {
            this.last = UserValueWrapper.Empty;
            this.reservoir.Reset();
        }

        private bool Merge(HistogramMetric other)
        {
            return this.reservoir.Merge(other.reservoir);
        }

        private static Reservoir SamplingTypeToReservoir(SamplingType samplingType)
        {
            switch (samplingType)
            {
                case SamplingType.FavourRecent: return new ExponentiallyDecayingReservoir();
                case SamplingType.LongTerm: return new UniformReservoir();
                case SamplingType.SlidingWindow: return new SlidingWindowReservoir();
            }
            throw new InvalidOperationException("Sampling type not implemented " + samplingType);
        }
    }
}
