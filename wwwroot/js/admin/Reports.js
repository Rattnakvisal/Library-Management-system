(function ($) {
  var state = {
    reportType: "borrowing",
    fromDate: "",
    toDate: "",
    page: 1,
    pageSize: 10,
    totalRows: 0,
    rows: [],
    requestId: 0,
  };

  var reportConfigs = {
    borrowing: {
      title: "Borrowing Report",
      headers: [
        { key: "id", label: "Borrow ID" },
        { key: "title", label: "Book Title" },
        { key: "borrowDate", label: "Borrow Date" },
        { key: "dueDate", label: "Due Date" },
        { key: "status", label: "Status", align: "text-end" },
      ],
      rowHtml: function (row) {
        return (
          "<tr>" +
          "<td>" + escapeHtml(row.id) + "</td>" +
          '<td><div class="book-title">' + escapeHtml(row.title) + "</div></td>" +
          "<td>" + escapeHtml(row.borrowDate) + "</td>" +
          "<td>" + escapeHtml(row.dueDate) + "</td>" +
          '<td class="text-end">' + getStatusPill(row.status) + "</td>" +
          "</tr>"
        );
      },
      csvValues: function (row) {
        return [row.id, row.title, row.borrowDate, row.dueDate, row.status];
      },
    },
    returns: {
      title: "Return Books Report",
      headers: [
        { key: "id", label: "Return ID" },
        { key: "title", label: "Book Title" },
        { key: "user", label: "User" },
        { key: "returnDate", label: "Return Date" },
        { key: "status", label: "Status", align: "text-end" },
      ],
      rowHtml: function (row) {
        return (
          "<tr>" +
          "<td>" + escapeHtml(row.id) + "</td>" +
          '<td><div class="book-title">' + escapeHtml(row.title) + "</div></td>" +
          "<td>" + escapeHtml(row.user) + "</td>" +
          "<td>" + escapeHtml(row.returnDate) + "</td>" +
          '<td class="text-end">' + getStatusPill(row.status) + "</td>" +
          "</tr>"
        );
      },
      csvValues: function (row) {
        return [row.id, row.title, row.user, row.returnDate, row.status];
      },
    },
    "most-borrowed": {
      title: "Most Borrowed Books Report",
      headers: [
        { key: "rank", label: "Rank" },
        { key: "title", label: "Book Title" },
        { key: "category", label: "Category" },
        { key: "total", label: "Total Borrowed" },
        { key: "status", label: "Status", align: "text-end" },
      ],
      rowHtml: function (row) {
        return (
          "<tr>" +
          "<td>#" + escapeHtml(row.rank) + "</td>" +
          '<td><div class="book-title">' + escapeHtml(row.title) + "</div></td>" +
          "<td>" + escapeHtml(row.category) + "</td>" +
          "<td>" + escapeHtml(row.total) + "</td>" +
          '<td class="text-end">' + getStatusPill(row.status) + "</td>" +
          "</tr>"
        );
      },
      csvValues: function (row) {
        return [row.rank, row.title, row.category, row.total, row.status];
      },
    },
    "fine-collection": {
      title: "Fine Collection Report",
      headers: [
        { key: "id", label: "Fine ID" },
        { key: "title", label: "Book Title" },
        { key: "user", label: "User" },
        { key: "amount", label: "Amount" },
        { key: "paid", label: "Paid", align: "text-end" },
      ],
      rowHtml: function (row) {
        return (
          "<tr>" +
          "<td>" + escapeHtml(row.id) + "</td>" +
          '<td><div class="book-title">' + escapeHtml(row.title) + "</div></td>" +
          "<td>" + escapeHtml(row.user) + "</td>" +
          "<td>" + formatCurrency(row.amount) + "</td>" +
          '<td class="text-end">' + getStatusPill(row.paid) + "</td>" +
          "</tr>"
        );
      },
      csvValues: function (row) {
        return [row.id, row.title, row.user, formatCurrency(row.amount), row.paid, row.paidDate || ""];
      },
    },
  };

  var $page = $(".dashboard-page[data-report-url]");
  var reportUrl = $page.data("report-url") || "/admin/managereport/data";

  function escapeHtml(value) {
    return String(value || "")
      .replace(/&/g, "&amp;")
      .replace(/</g, "&lt;")
      .replace(/>/g, "&gt;")
      .replace(/\"/g, "&quot;")
      .replace(/'/g, "&#039;");
  }

  function escapeCsv(value) {
    var text = String(value == null ? "" : value);
    return '"' + text.replace(/"/g, '""') + '"';
  }

  function formatCurrency(amount) {
    var number = Number(amount || 0);
    return "$" + number.toFixed(2);
  }

  function getStatusPill(status) {
    var label = String(status || "");
    var normalized = label.toLowerCase();
    var cssClass = "borrowed";

    if (normalized === "returned" || normalized === "paid") {
      cssClass = "returned";
    } else if (normalized === "overdue" || normalized === "unpaid") {
      cssClass = "overdue";
    }

    return '<span class="status-pill ' + cssClass + '">' + escapeHtml(label) + "</span>";
  }

  function getReportConfig(type) {
    return reportConfigs[type] || reportConfigs.borrowing;
  }

  function setSelectButtonText($button, text) {
    $button.html('<span>' + escapeHtml(text) + '</span><i class="bi bi-chevron-down caret"></i>');
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

  function toggleSelect(containerId) {
    var $container = $("#" + containerId);
    var isOpen = $container.hasClass("open");

    closeAllSelects(containerId);

    if (!isOpen) {
      $container.addClass("open");
      $container.find(".options").show();
    } else {
      $container.removeClass("open");
      $container.find(".options").hide();
    }
  }

  function setLoadingState() {
    var config = getReportConfig(state.reportType);
    var columnCount = config.headers.length;

    $(".report-table thead").html(renderTableHeader(config));
    $(".report-table tbody").html(
      '<tr><td colspan="' +
        columnCount +
        '" class="text-center py-4 text-muted">Loading report data...</td></tr>'
    );
    $("#reportMeta").text("Loading...");
    $("#reportPagination").empty();
  }

  function renderTableHeader(config) {
    var html = "<tr>";
    for (var i = 0; i < config.headers.length; i += 1) {
      var header = config.headers[i];
      var alignClass = header.align ? " " + header.align : "";
      html += '<th class="' + alignClass.trim() + '">' + escapeHtml(header.label) + "</th>";
    }
    html += "</tr>";
    return html;
  }

  function renderTable(type, rows) {
    var config = getReportConfig(type);
    var $thead = $(".report-table thead");
    var $tbody = $(".report-table tbody");

    $thead.html(renderTableHeader(config));

    if (!rows.length) {
      $tbody.html(
        '<tr><td colspan="' +
          config.headers.length +
          '" class="text-center py-4 text-muted">No report data found.</td></tr>'
      );
      return;
    }

    var bodyHtml = "";
    for (var i = 0; i < rows.length; i += 1) {
      bodyHtml += config.rowHtml(rows[i]);
    }

    $tbody.html(bodyHtml);
  }

  function renderError(message) {
    var config = getReportConfig(state.reportType);
    $(".report-table thead").html(renderTableHeader(config));
    $(".report-table tbody").html(
      '<tr><td colspan="' +
        config.headers.length +
        '" class="text-center py-4 text-danger">' +
        escapeHtml(message) +
        "</td></tr>"
    );
    $("#reportMeta").text("Unable to load report data.");
    $("#reportPagination").empty();
  }

  function getVisiblePages(currentPage, totalPages) {
    var pages = [];

    if (totalPages <= 7) {
      for (var p = 1; p <= totalPages; p += 1) {
        pages.push(p);
      }
      return pages;
    }

    pages.push(1);

    if (currentPage > 3) {
      pages.push("...");
    }

    var start = Math.max(2, currentPage - 1);
    var end = Math.min(totalPages - 1, currentPage + 1);

    for (var i = start; i <= end; i += 1) {
      pages.push(i);
    }

    if (currentPage < totalPages - 2) {
      pages.push("...");
    }

    pages.push(totalPages);
    return pages;
  }

  function renderPagination(currentPage, totalPages, totalRows) {
    var $pagination = $("#reportPagination");
    $pagination.empty();

    var safeTotalPages = Math.max(1, Number(totalPages || 1));
    var safeCurrentPage = Math.min(Math.max(1, Number(currentPage || 1)), safeTotalPages);
    var safeTotalRows = Math.max(0, Number(totalRows || 0));

    var startIndex = safeTotalRows === 0 ? 0 : (safeCurrentPage - 1) * state.pageSize + 1;
    var endIndex = safeTotalRows === 0 ? 0 : Math.min(safeCurrentPage * state.pageSize, safeTotalRows);
    $("#reportMeta").text("Showing " + startIndex + "-" + endIndex + " of " + safeTotalRows + " records");

    var prevDisabled = safeCurrentPage <= 1 ? " disabled" : "";
    $pagination.append(
      '<li class="page-item' + prevDisabled + '"><a class="page-link" href="#" data-page="' +
        (safeCurrentPage - 1) +
        '">Previous</a></li>'
    );

    var visiblePages = getVisiblePages(safeCurrentPage, safeTotalPages);
    for (var i = 0; i < visiblePages.length; i += 1) {
      var page = visiblePages[i];
      if (page === "...") {
        $pagination.append('<li class="page-item disabled"><span class="page-link">...</span></li>');
        continue;
      }

      var activeClass = page === safeCurrentPage ? " active" : "";
      $pagination.append(
        '<li class="page-item' + activeClass + '"><a class="page-link" href="#" data-page="' + page + '">' + page + "</a></li>"
      );
    }

    var nextDisabled = safeCurrentPage >= safeTotalPages ? " disabled" : "";
    $pagination.append(
      '<li class="page-item' + nextDisabled + '"><a class="page-link" href="#" data-page="' +
        (safeCurrentPage + 1) +
        '">Next</a></li>'
    );
  }

  function showWarning(message) {
    if (window.Swal && typeof window.Swal.fire === "function") {
      window.Swal.fire({ icon: "warning", text: message });
      return;
    }

    window.alert(message);
  }

  function validateDateRange(showAlert) {
    if (!state.fromDate || !state.toDate) {
      return true;
    }

    if (state.fromDate <= state.toDate) {
      return true;
    }

    if (showAlert) {
      showWarning("From Date cannot be later than To Date.");
    }

    return false;
  }

  async function loadReport() {
    if (!validateDateRange(true)) {
      return;
    }

    var requestId = state.requestId + 1;
    state.requestId = requestId;

    setLoadingState();

    var query = new URLSearchParams({
      reportType: state.reportType,
      page: String(state.page),
      pageSize: String(state.pageSize),
    });

    if (state.fromDate) {
      query.append("fromDate", state.fromDate);
    }

    if (state.toDate) {
      query.append("toDate", state.toDate);
    }

    try {
      var response = await fetch(reportUrl + "?" + query.toString(), {
        method: "GET",
        headers: { Accept: "application/json" },
      });

      var payload = await response.json().catch(function () {
        return {};
      });

      if (requestId !== state.requestId) {
        return;
      }

      if (!response.ok || !payload.success) {
        throw new Error(payload.message || "Failed to load report data.");
      }

      state.page = Number(payload.page || 1);
      state.pageSize = Number(payload.pageSize || state.pageSize || 10);
      state.totalRows = Number(payload.totalRows || 0);
      state.rows = Array.isArray(payload.rows) ? payload.rows : [];

      renderTable(state.reportType, state.rows);
      renderPagination(state.page, payload.totalPages, state.totalRows);
    } catch (error) {
      var message = (error && error.message) || "Failed to load report data.";
      renderError(message);
    }
  }

  function downloadCsv() {
    var config = getReportConfig(state.reportType);

    if (!state.rows.length) {
      showWarning("There is no data to export.");
      return;
    }

    var lines = [];
    var headerRow = [];
    for (var i = 0; i < config.headers.length; i += 1) {
      headerRow.push(escapeCsv(config.headers[i].label));
    }

    if (state.reportType === "fine-collection") {
      headerRow.push(escapeCsv("Paid Date"));
    }

    lines.push(headerRow.join(","));

    for (var r = 0; r < state.rows.length; r += 1) {
      var values = config.csvValues(state.rows[r]);
      var escaped = [];
      for (var c = 0; c < values.length; c += 1) {
        escaped.push(escapeCsv(values[c]));
      }
      lines.push(escaped.join(","));
    }

    var csvContent = "\uFEFF" + lines.join("\r\n");
    var blob = new Blob([csvContent], { type: "text/csv;charset=utf-8;" });
    var downloadUrl = URL.createObjectURL(blob);
    var link = document.createElement("a");
    var fileDate = new Date().toISOString().slice(0, 10);

    link.href = downloadUrl;
    link.download = state.reportType + "-report-" + fileDate + ".csv";
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
    URL.revokeObjectURL(downloadUrl);
  }

  function handleExport(type) {
    if (type === "excel") {
      downloadCsv();
      return;
    }

    if (type === "pdf") {
      window.print();
    }
  }

  function bindSelectHandlers() {
    $("#reportTypeBtn").on("click", function (e) {
      e.preventDefault();
      e.stopPropagation();
      toggleSelect("reportTypeSelect");
    });

    $("#exportTypeBtn").on("click", function (e) {
      e.preventDefault();
      e.stopPropagation();
      toggleSelect("exportTypeSelect");
    });

    $("#reportTypeSelect .options").on("click", "li", function (e) {
      e.preventDefault();
      e.stopPropagation();

      var $option = $(this);
      var type = String($option.data("value") || "borrowing");
      var text = $.trim($option.text()) || "Borrowing Report";

      state.reportType = type;
      state.page = 1;
      setSelectButtonText($("#reportTypeBtn"), text);
      closeAllSelects();
      loadReport();
    });

    $("#exportTypeSelect .options").on("click", "li", function (e) {
      e.preventDefault();
      e.stopPropagation();

      var $option = $(this);
      var type = String($option.data("value") || "");
      var text = $.trim($option.text()) || "Export Type";

      setSelectButtonText($("#exportTypeBtn"), text);
      closeAllSelects();
      handleExport(type);

      window.setTimeout(function () {
        setSelectButtonText($("#exportTypeBtn"), "Export Type");
      }, 800);
    });
  }

  function bindDateFilters() {
    $("#fromDate, #toDate").on("change", function () {
      state.fromDate = String($("#fromDate").val() || "");
      state.toDate = String($("#toDate").val() || "");

      if (!validateDateRange(true)) {
        return;
      }

      state.page = 1;
      loadReport();
    });
  }

  function bindPagination() {
    $(document).on("click", "#reportPagination .page-link", function (e) {
      e.preventDefault();

      var $item = $(this).closest(".page-item");
      if ($item.hasClass("disabled") || $item.hasClass("active")) {
        return;
      }

      var targetPage = Number($(this).data("page") || 0);
      if (!targetPage || targetPage < 1 || targetPage === state.page) {
        return;
      }

      state.page = targetPage;
      loadReport();
    });
  }

  $(function () {
    setSelectButtonText($("#reportTypeBtn"), getReportConfig(state.reportType).title);
    setSelectButtonText($("#exportTypeBtn"), "Export Type");

    bindSelectHandlers();
    bindDateFilters();
    bindPagination();

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

    loadReport();
  });
})(jQuery);
