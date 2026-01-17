(function ($) {
  // sample datasets for each report type
  var sampleData = {
    borrowing: [
      {
        id: "009",
        title:
          "Cambridge English for Engineering Students Book (SAE) with CDS(2) with CDS(2)",
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

  // dropdown open/close + select
  function initSelect(containerId, btnId) {
    var $container = $("#" + containerId);
    var $btn = $("#" + btnId);
    var $opts = $container.find(".options");

    // open / close
    $btn.on("click", function (e) {
      e.preventDefault();

      // close other dropdowns
      $(".custom-select .options, .export-select .options").hide();

      $opts.toggle();
    });

    // select item
    $opts.on("click", "li", function () {
      var $li = $(this);
      var text = $.trim($li.text());
      var type = $li.data("value");

      $btn.html(text + ' <span class="caret">▾</span>');
      $opts.hide();

      // only report type should render table
      if (containerId === "reportTypeSelect") {
        renderReport(type);
      }
    });
  }

  $(function () {
    initSelect("reportTypeSelect", "reportTypeBtn");
    initSelect("exportTypeSelect", "exportTypeBtn");

    // default load
    renderReport("borrowing");

    // close dropdowns when click outside
    $(document).on("click", function (e) {
      if (!$(e.target).closest(".custom-select, .export-select").length) {
        $(".custom-select .options, .export-select .options").hide();
      }
    });
  });
})(jQuery);
