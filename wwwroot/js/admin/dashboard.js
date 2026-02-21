$(function () {
  const categoryPalette = [
    "#8B5E53",
    "#1D4ED8",
    "#22C55E",
    "#EF4444",
    "#0EA5E9",
    "#F59E0B",
    "#7C3AED",
    "#14B8A6",
    "#EC4899",
    "#334155",
  ];

  $(".qtab").on("click", function () {
    const $this = $(this);
    const target = $this.data("target");

    $(".qtab").removeClass("active");
    $this.addClass("active");

    $(".qtab-content").removeClass("show");
    $(target).addClass("show");
  });

  function getCategoryChartData() {
    const source = window.dashboardCategoryChart || {};
    const labels = Array.isArray(source.labels) ? source.labels : [];
    const counts = Array.isArray(source.counts) ? source.counts : [];

    const normalized = labels
      .map((label, index) => ({
        label: String(label || "").trim(),
        count: Number(counts[index] || 0),
      }))
      .filter((x) => x.label.length > 0 && x.count > 0);

    if (normalized.length === 0) {
      return {
        labels: ["No Data"],
        counts: [1],
        colors: ["#CBD5E1"],
        empty: true,
      };
    }

    return {
      labels: normalized.map((x) => x.label),
      counts: normalized.map((x) => x.count),
      colors: normalized.map((_, i) => categoryPalette[i % categoryPalette.length]),
      empty: false,
    };
  }

  function renderCategoryLegend(labels, counts, colors, isEmpty) {
    const legend = document.getElementById("categoryLegend");
    if (!legend) {
      return;
    }

    legend.innerHTML = "";

    if (isEmpty) {
      const li = document.createElement("li");
      li.className = "legend-empty";
      li.textContent = "No category data";
      legend.appendChild(li);
      return;
    }

    labels.forEach((label, i) => {
      const li = document.createElement("li");
      const dot = document.createElement("span");
      const text = document.createElement("span");

      dot.className = "dot";
      dot.style.backgroundColor = colors[i];
      text.textContent = `${label} (${counts[i]})`;

      li.appendChild(dot);
      li.appendChild(text);
      legend.appendChild(li);
    });
  }

  function createCategoryChart(ctx) {
    const chartData = getCategoryChartData();

    renderCategoryLegend(
      chartData.labels,
      chartData.counts,
      chartData.colors,
      chartData.empty
    );

    return new Chart(ctx, {
      type: "doughnut",
      data: {
        labels: chartData.labels,
        datasets: [
          {
            data: chartData.counts,
            backgroundColor: chartData.colors,
            borderWidth: 0,
          },
        ],
      },
      options: {
        responsive: true,
        maintainAspectRatio: false,
        cutout: "50%",
        plugins: {
          legend: { display: false },
          tooltip: {
            callbacks: {
              label: (context) => {
                if (chartData.empty) {
                  return "No data";
                }
                const value = context.parsed ?? 0;
                return `${context.label}: ${value} book(s)`;
              },
            },
          },
        },
      },
    });
  }

  function createTrendsChart(ctx) {
    return new Chart(ctx, {
      type: "bar",
      data: {
        labels: ["1", "2", "3", "4", "5", "6", "7", "8", "9", "10", "11", "12"],
        datasets: [
          {
            label: "Months",
            data: [120, 140, 95, 110, 80, 175, 160, 90, 125, 110, 140, 155],
            backgroundColor: "#2b6da3",
          },
        ],
      },
      options: {
        responsive: true,
        maintainAspectRatio: false,
        scales: {
          x: { grid: { display: false } },
          y: { beginAtZero: true },
        },
        plugins: {
          legend: { display: false },
        },
      },
    });
  }

  try {
    const $cat = $("#chartCategories");
    const $trends = $("#chartTrends");

    if ($cat.length && window.Chart) {
      createCategoryChart($cat[0].getContext("2d"));
    }

    if ($trends.length && window.Chart) {
      createTrendsChart($trends[0].getContext("2d"));
    }
  } catch (e) {
    console.warn("Chart init failed", e);
  }
});
