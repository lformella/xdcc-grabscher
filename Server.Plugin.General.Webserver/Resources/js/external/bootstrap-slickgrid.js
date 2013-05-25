(function ($) {

  "use strict"; // jshint ;_;

  /*global Slick: true*/

  /* SlickGrid PUBLIC CLASS DEFINITION
   * ================================= */

  var SlickGrid = function ( element, options ) {
    element = $(element);
    var cookedOptions = $.extend(true, {},
      $.fn.slickgrid.defaults, options);
    this.init('slickgrid', element, cookedOptions);
  };

  SlickGrid.prototype = {

    constructor: SlickGrid,

    init: function (type, element, options) {
      var self = this;
      this.element = $(element);
      this.wrapperOptions = $.extend(true, options, {
        // always render without ui-* css styles
        slickGridOptions: {jQueryUiStyles: false}
      });
      this.postInit();
    },

    postInit: function () {
      // Call the provided hook to post-process.
      (this.wrapperOptions.handleCreate || this.handleCreate).apply(this);
    },

    handleCreate: function () {
      // Create a simple grid configuration.
      //
      // This handler will run after the options
      // have been preprocessed. It can be overridden by passing
      // the handleCreate option at creation time.
      //
      // Variables you can access from this handler:
      //
      // this:                  will equal to the SlickGrid object instance
      // this.element:          the element to bind the grid to
      // this.wrapperOptions:   options passed to this object at creation
      //
      // this.wrapperOptions.slickGridOptions: options for Slick.Grid
      //
      var o = this.wrapperOptions;
      var grid = new Slick.Grid(this.element, o.data,
        o.columns, o.slickGridOptions);
    }

  };

  /* SlickGrid PLUGIN DEFINITION */

  $.fn.slickgrid = function (option) {
    return this.each(function () {
      var $this = $(this),
        data = $this.data('slickgrid'),
        options = typeof option == 'object' && option;
      if (!data) {
        $this.data('slickgrid', (data = new SlickGrid(this, options)));
      }
      if (typeof option == 'string') {
        data[option]();
      }
    });
  };

  $.fn.slickgrid.Constructor = SlickGrid;

  $.fn.slickgrid.defaults = {
    slickGridOptions: {},
    columns: [],         // Column meta data in SlickGrid's format.
    handleCreate: null   // This handler is called after the grid is created,
                         // and it can be used to customize the grid.
          // Variables you can access from this handler:
          //
          // this:                  will equal to the SlickGrid object instance
          // this.element:          the element to bind the grid to
          // this.wrapperOptions:   options passed to this object at creation
  };

})(window.jQuery);
