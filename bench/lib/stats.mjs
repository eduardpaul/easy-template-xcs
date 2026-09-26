export function summarize(samples) {
    const sorted = [...samples].sort((a, b) => a - b);
    const n = sorted.length;
    const mean = sorted.reduce((sum, x) => sum + x, 0) / n;
    const variance = sorted.reduce((sum, x) => sum + (x - mean) ** 2, 0) / Math.max(1, n - 1);
    return {
        n,
        mean,
        median: quantile(sorted, 0.5),
        p95: quantile(sorted, 0.95),
        min: sorted[0],
        max: sorted[n - 1],
        stddev: Math.sqrt(variance),
        opsPerSec: 1000 / mean
    };
}

function quantile(sorted, q) {
    const pos = (sorted.length - 1) * q;
    const lo = Math.floor(pos);
    const hi = Math.ceil(pos);
    return sorted[lo] + (sorted[hi] - sorted[lo]) * (pos - lo);
}
