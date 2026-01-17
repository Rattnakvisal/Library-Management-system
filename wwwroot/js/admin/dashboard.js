document.addEventListener("DOMContentLoaded", () => {
  const tabs = document.querySelectorAll(".qtab");
  const contents = document.querySelectorAll(".qtab-content");

  tabs.forEach((t) => {
    t.addEventListener("click", () => {
      tabs.forEach((x) => x.classList.remove("active"));
      t.classList.add("active");

      const target = t.getAttribute("data-target");
      contents.forEach((c) => c.classList.remove("show"));
      document.querySelector(target)?.classList.add("show");
    });
  });
});
