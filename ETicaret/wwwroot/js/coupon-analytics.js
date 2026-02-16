window.couponAnalytics = (function () {
    const charts = {};

    function destroyUsageChart(canvasId) {
        const chart = charts[canvasId];
        if (chart) {
            chart.destroy();
            delete charts[canvasId];
        }
    }

    function renderUsageChart(canvasId, labels, data) {
        if (!window.Chart) return;

        destroyUsageChart(canvasId);

        const canvas = document.getElementById(canvasId);
        if (!canvas) return;

        const ctx = canvas.getContext("2d");
        charts[canvasId] = new Chart(ctx, {
            type: "line",
            data: {
                labels,
                datasets: [{
                    label: "Kullanım",
                    data,
                    borderColor: "#2563eb",
                    backgroundColor: "rgba(37, 99, 235, 0.15)",
                    fill: true,
                    tension: 0.35,
                    pointRadius: 3,
                    pointHoverRadius: 5
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                plugins: {
                    legend: { display: false }
                },
                scales: {
                    y: {
                        beginAtZero: true,
                        ticks: { precision: 0 }
                    }
                }
            }
        });
    }

    return {
        renderUsageChart,
        destroyUsageChart
    };
})();
