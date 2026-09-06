(function () {
  var tree = [
    { href: "index.html", label: "Home" },
    { section: "Getting started" },
    { href: "getting-started/index.html", label: "Start here" },
    { href: "getting-started/metadata.html", label: "Metadata source" },
    { section: "Library" },
    { href: "library/index.html", label: "Library overview" },
    { href: "library/add-authors-and-books.html", label: "Add authors and books" },
    { href: "library/wanted-formats.html", label: "Wanted formats" },
    { href: "library/unmapped-files.html", label: "Unmapped files" },
    { href: "library/wanted.html", label: "Missing and cutoff" },
    { section: "Settings" },
    { href: "settings/index.html", label: "Settings overview" },
    { href: "settings/quality-profiles.html", label: "Quality profiles" },
    { href: "settings/naming.html", label: "File naming" },
    { href: "settings/media-management.html", label: "Media management" }
  ];

  function depthPrefix(href) {
    var path = window.location.pathname.replace(/\\/g, "/");
    var inSection = /\/(getting-started|library|settings)\//.test(path);
    return inSection ? "../" : "";
  }

  var prefix = depthPrefix();
  var nav = document.getElementById("guide-nav");
  if (!nav) {
    return;
  }

  var current = document.body.getAttribute("data-page") || "";
  var html = "";
  tree.forEach(function (item) {
    if (item.section) {
      html += '<div class="section">' + item.section + "</div>";
      return;
    }
    var href = prefix + item.href;
    var currentAttr = item.href === current ? ' aria-current="page"' : "";
    html += '<a href="' + href + '"' + currentAttr + ">" + item.label + "</a>";
  });
  nav.innerHTML = html;

  var toggle = document.getElementById("nav-toggle");
  if (toggle) {
    toggle.addEventListener("click", function () {
      nav.classList.toggle("open");
    });
  }
})();
