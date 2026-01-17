$(function () {
  /* ==========================
     QUICK TABS
  ========================== */
  $(".qtab").on("click", function () {
    const $this = $(this);
    const target = $this.data("target");

    $(".qtab").removeClass("active");
    $this.addClass("active");

    $(".qtab-content").removeClass("show");
    $(target).addClass("show");
  });

  /* ==========================
     CHARTS
  ========================== */
  function createCategoryChart(ctx) {
    return new Chart(ctx, {
      type: "doughnut",
      data: {
        labels: ["Education", "Art and Culture", "Religious", "Other"],
        datasets: [
          {
            data: [420, 220, 300, 260],
            backgroundColor: ["#8b5e53", "#1534ff", "#1dd13a", "#ff2b2b"],
            borderWidth: 0,
          },
        ],
      },
      options: {
        responsive: true,
        maintainAspectRatio: false,
        plugins: {
          legend: { display: false },
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

  /* ==========================
     INIT CHARTS SAFELY
  ========================== */
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
