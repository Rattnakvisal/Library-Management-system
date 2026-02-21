(function ($) {
  var sampleData = {
    borrowing: [
      {
        id: "009",
        title: "Cambridge English for Engineering Students Book (SAE) with CDS(2)",
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
        title: "Cambridge English for Engineering Students Book (SAE) with CDS(2)",
        user: "John Doe",
        returnDate: "12/12/25",
      },
      {
        id: "R-002",
        title: "Learning ASP.NET Core",
        user: "Jane Smith",
        returnDate: "11/20/25",
      },
    ],
    "most-borrowed": [
      {
        rank: 1,
        title: "Cambridge English for Engineering Students Book (SAE) with CDS(2)",
        category: "Education",
        total: 90,
      },
      {
        rank: 2,
        title: "C# in Depth",
        category: "Programming",
        total: 64,
      },
    ],
    "fine-collection": [
      {
        id: "F-001",
        title: "Cambridge English for Engineering Students Book (SAE) with CDS(2)",
        user: "John Doe",
        amount: "$1.00",
        paid: "Yes",
        paidDate: "12/12/25",
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

  function escapeHtml(value) {
    return String(value || "")
      .replace(/&/g, "&amp;")
      .replace(/</g, "&lt;")
      .replace(/>/g, "&gt;")
      .replace(/\"/g, "&quot;")
      .replace(/'/g, "&#039;");
  }

  function getStatusPill(status) {
    var normalized = String(status || "").toLowerCase();
    var cssClass = "borrowed";

    if (normalized === "returned" || normalized === "yes" || normalized === "paid") {
      cssClass = "returned";
    } else if (normalized === "overdue" || normalized === "no" || normalized === "unpaid") {
      cssClass = "overdue";
    }

    return '<span class="status-pill ' + cssClass + '">' + escapeHtml(status) + "</span>";
  }

  function setTableRows(type) {
    var $thead = $(".report-table thead");
    var $tbody = $(".report-table tbody");
    var rows = sampleData[type] || [];
    var headerHtml = "";
    var bodyHtml = "";

    if (type === "returns") {
      headerHtml =
        "<tr>" +
        "<th>Return ID</th>" +
        "<th>Book Title</th>" +
        "<th>User</th>" +
        "<th>Return Date</th>" +
        '<th class="text-end">Status</th>' +
        "</tr>";

      for (var i = 0; i < rows.length; i += 1) {
        bodyHtml +=
          "<tr>" +
          "<td>" + escapeHtml(rows[i].id) + "</td>" +
          '<td><div class="book-title">' + escapeHtml(rows[i].title) + "</div></td>" +
          "<td>" + escapeHtml(rows[i].user) + "</td>" +
          "<td>" + escapeHtml(rows[i].returnDate) + "</td>" +
          '<td class="text-end">' + getStatusPill("Returned") + "</td>" +
          "</tr>";
      }
    } else if (type === "most-borrowed") {
      headerHtml =
        "<tr>" +
        "<th>Rank</th>" +
        "<th>Book Title</th>" +
        "<th>Category</th>" +
        "<th>Total Borrowed</th>" +
        '<th class="text-end">Status</th>' +
        "</tr>";

      for (var j = 0; j < rows.length; j += 1) {
        bodyHtml +=
          "<tr>" +
          "<td>#" + escapeHtml(rows[j].rank) + "</td>" +
          '<td><div class="book-title">' + escapeHtml(rows[j].title) + "</div></td>" +
          "<td>" + escapeHtml(rows[j].category) + "</td>" +
          "<td>" + escapeHtml(rows[j].total) + "</td>" +
          '<td class="text-end">' + getStatusPill("Borrowed") + "</td>" +
          "</tr>";
      }
    } else if (type === "fine-collection") {
      headerHtml =
        "<tr>" +
        "<th>Fine ID</th>" +
        "<th>Book Title</th>" +
        "<th>User</th>" +
        "<th>Amount</th>" +
        '<th class="text-end">Paid</th>' +
        "</tr>";

      for (var k = 0; k < rows.length; k += 1) {
        var paidText = String(rows[k].paid || "").toLowerCase() === "yes" ? "Paid" : "Unpaid";

        bodyHtml +=
          "<tr>" +
          "<td>" + escapeHtml(rows[k].id) + "</td>" +
          '<td><div class="book-title">' + escapeHtml(rows[k].title) + "</div></td>" +
          "<td>" + escapeHtml(rows[k].user) + "</td>" +
          "<td>" + escapeHtml(rows[k].amount) + "</td>" +
          '<td class="text-end">' + getStatusPill(paidText) + "</td>" +
          "</tr>";
      }
    } else {
      headerHtml =
        "<tr>" +
        "<th>Borrow ID</th>" +
        "<th>Book Title</th>" +
        "<th>Borrow Date</th>" +
        "<th>Due Date</th>" +
        '<th class="text-end">Status</th>' +
        "</tr>";

      for (var l = 0; l < rows.length; l += 1) {
        bodyHtml +=
          "<tr>" +
          "<td>" + escapeHtml(rows[l].id) + "</td>" +
          '<td><div class="book-title">' + escapeHtml(rows[l].title) + "</div></td>" +
          "<td>" + escapeHtml(rows[l].borrowDate) + "</td>" +
          "<td>" + escapeHtml(rows[l].dueDate) + "</td>" +
          '<td class="text-end">' + getStatusPill(rows[l].status) + "</td>" +
          "</tr>";
      }
    }

    if (!rows.length) {
      bodyHtml = '<tr><td colspan="5" class="text-center py-4">No report data found.</td></tr>';
    }

    $thead.html(headerHtml);
    $tbody.html(bodyHtml);
  }

  function renderReport(type) {
    setTableRows(type || "borrowing");
  }

  function closeAllSelects(exceptId) {
    $(".custom-select, .export-select").each(function () {
      var $select = $(this);
      if (exceptId && $select.attr("id") === exceptId) {
        return;
      }

      $select.removeClass("open");
      $select.find(".options").hide();
    });
  }

  function initSelect(containerId, btnId) {
    var $container = $("#" + containerId);
    var $btn = $("#" + btnId);
    var $opts = $container.find(".options");

    $btn.on("click", function (e) {
      e.preventDefault();
      e.stopPropagation();

      var isOpen = $container.hasClass("open");
      closeAllSelects(containerId);

      if (!isOpen) {
        $container.addClass("open");
        $opts.show();
      } else {
        $container.removeClass("open");
        $opts.hide();
      }
    });

    $opts.on("click", "li", function (e) {
      e.preventDefault();
      e.stopPropagation();

      var $li = $(this);
      var text = $.trim($li.text());
      var type = $li.data("value");

      $btn.html('<span>' + text + '</span><i class="bi bi-chevron-down caret"></i>');
      $container.removeClass("open");
      $opts.hide();

      if (containerId === "reportTypeSelect") {
        renderReport(type);
      }
    });
  }

  $(function () {
    initSelect("reportTypeSelect", "reportTypeBtn");
    initSelect("exportTypeSelect", "exportTypeBtn");

    renderReport("borrowing");

    $(document).on("click", function (e) {
      if (!$(e.target).closest(".custom-select, .export-select").length) {
        closeAllSelects();
      }
    });

    $(document).on("keydown", function (e) {
      if (e.key === "Escape") {
        closeAllSelects();
      }
    });
  });
})(jQuery);
