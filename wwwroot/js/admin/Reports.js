(function () {
  // sample datasets for each report type
  var sampleData = {
    borrowing: [
      {
        id: "009",
        title:
          "Cambridge English for Engineering Students Book (SAE) with CDS(2)",
        borrowDate: "11/14/25",
        dueDate: "11/19/25",
        status: "Borrowed",
      },
      {
        id: "010",
        title: "Learning ASP.NET Core",
        borrowDate: "11/10/25",
        dueDate: "11/17/25",
        status: "Returned",
      },
      {
        id: "011",
        title: "C# in Depth",
        borrowDate: "11/05/25",
        dueDate: "11/12/25",
        status: "Overdue",
      },
    ],
    returns: [
      {
        id: "R-001",
        title:
          "Cambridge English for Engineering Students Book (SAE) with CDS(2)",
        user: "John Doe",
        returnDate: "2025/12/12",
      },
      {
        id: "R-002",
        title: "Learning ASP.NET Core",
        user: "Jane Smith",
        returnDate: "2025/11/20",
      },
    ],
    "most-borrowed": [
      {
        rank: 1,
        title:
          "Cambridge English for Engineering Students Book (SAE) with CDS(2)",
        category: "Fiction",
        total: 90,
      },
      { rank: 2, title: "C# in Depth", category: "Programming", total: 64 },
    ],
    "fine-collection": [
      {
        id: "F-001",
        title:
          "Cambridge English for Engineering Students Book (SAE) with CDS(2)",
        user: "John Doe",
        amount: "$1.00",
        paid: "Yes",
        paidDate: "2025/12/12",
      },
      {
        id: "F-002",
        title: "C# in Depth",
        user: "Jane Smith",
        amount: "$2.50",
        paid: "No",
        paidDate: "",
      },
    ],
  };

  function renderReport(type) {
    var wrapper = document.querySelector(".report-table-wrapper");
    if (!wrapper) return;

    var html = "";

    if (type === "borrowing") {
      html +=
        '<table class="table report-table">\n<thead>\n<tr>\n<th>Borrow ID</th>\n<th>Book Tittle</th>\n<th>Borrow Date</th>\n<th>Due Date</th>\n<th class="text-end">Status</th>\n</tr>\n</thead>\n<tbody>\n';
      sampleData.borrowing.forEach(function (r) {
        html +=
          "<tr>\n<td>" +
          r.id +
          '</td>\n<td><div class="book-title">' +
          r.title +
          "</div></td>\n<td>" +
          r.borrowDate +
          "</td>\n<td>" +
          r.dueDate +
          '</td>\n<td class="text-end"><span class="status-pill borrowed">' +
          r.status +
          "</span></td>\n</tr>\n";
      });
      html += "</tbody>\n</table>";
    } else if (type === "returns") {
      html +=
        '<table class="table report-table">\n<thead>\n<tr>\n<th>Return ID</th>\n<th>Book Tittle</th>\n<th>User</th>\n<th>Return Date</th>\n</tr>\n</thead>\n<tbody>\n';
      sampleData.returns.forEach(function (r) {
        html +=
          "<tr>\n<td>" +
          r.id +
          '</td>\n<td><div class="book-title">' +
          r.title +
          "</div></td>\n<td>" +
          r.user +
          "</td>\n<td>" +
          r.returnDate +
          "</td>\n</tr>\n";
      });
      html += "</tbody>\n</table>";
    } else if (type === "most-borrowed") {
      html +=
        '<table class="table report-table">\n<thead>\n<tr>\n<th>Rank</th>\n<th>Book Tittle</th>\n<th>Category</th>\n<th>Total Borrowed</th>\n</tr>\n</thead>\n<tbody>\n';
      sampleData["most-borrowed"].forEach(function (r) {
        html +=
          "<tr>\n<td>" +
          r.rank +
          '</td>\n<td><div class="book-title">' +
          r.title +
          "</div></td>\n<td>" +
          r.category +
          "</td>\n<td>" +
          r.total +
          "</td>\n</tr>\n";
      });
      html += "</tbody>\n</table>";
    } else if (type === "fine-collection") {
      html +=
        '<table class="table report-table">\n<thead>\n<tr>\n<th>Fine ID</th>\n<th>Book Tittle</th>\n<th>User</th>\n<th>Amount</th>\n<th>Paid</th>\n<th>Paid Date</th>\n</tr>\n</thead>\n<tbody>\n';
      sampleData["fine-collection"].forEach(function (r) {
        html +=
          "<tr>\n<td>" +
          r.id +
          '</td>\n<td><div class="book-title">' +
          r.title +
          "</div></td>\n<td>" +
          r.user +
          "</td>\n<td>" +
          r.amount +
          "</td>\n<td>" +
          r.paid +
          "</td>\n<td>" +
          r.paidDate +
          "</td>\n</tr>\n";
      });
      html += "</tbody>\n</table>";
    }

    // replace table area but keep pagination below (if present)
    var pagination = wrapper.querySelector(".pagination-wrapper");
    wrapper.innerHTML = html;
    if (pagination) wrapper.appendChild(pagination);
  }

  function toggleOptions(containerId, btnId) {
    var container = document.getElementById(containerId);
    if (!container) return;
    var btn = document.getElementById(btnId);
    var opts = container.querySelector(".options");

    btn &&
      btn.addEventListener("click", function (e) {
        e.preventDefault();
        var open = opts.style.display === "block";
        document
          .querySelectorAll(".custom-select .options, .export-select .options")
          .forEach(function (o) {
            o.style.display = "none";
          });
        opts.style.display = open ? "none" : "block";
      });

    // select option and render appropriate table
    opts &&
      opts.addEventListener("click", function (e) {
        var li = e.target.closest("li");
        if (!li) return;
        var text = li.textContent.trim();
        if (btn) btn.innerHTML = text + ' <span class="caret">▾</span>';
        opts.style.display = "none";
        var type = li.getAttribute("data-value");
        renderReport(type);
      });
  }

  // initialize selects and default table
  document.addEventListener("DOMContentLoaded", function () {
    toggleOptions("reportTypeSelect", "reportTypeBtn");
    toggleOptions("exportTypeSelect", "exportTypeBtn");

    // render default
    renderReport("borrowing");

    // close dropdowns when clicking outside
    document.addEventListener("click", function (e) {
      if (
        !e.target.closest(".custom-select") &&
        !e.target.closest(".export-select")
      ) {
        document
          .querySelectorAll(".custom-select .options, .export-select .options")
          .forEach(function (o) {
            o.style.display = "none";
          });
      }
    });

    // simple pagination active state (delegated)
    document.addEventListener("click", function (e) {
      var link = e.target.closest(".pagination .page-link");
      if (!link) return;
      e.preventDefault();
      var li = link.parentElement;
      if (li.classList.contains("disabled")) return;
      document.querySelectorAll(".pagination .page-item").forEach(function (i) {
        i.classList.remove("active");
      });
      li.classList.add("active");
    });
  });
})();
