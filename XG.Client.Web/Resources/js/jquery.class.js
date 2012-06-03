/*
 Based on Alex Arnell's inheritance implementation.
 Ported by Christopher Blum (http://github.com/tiff) from Prototype (https://github.com/sstephenson/prototype) to be compatible with jQuery
 */

var Class = (function($) {

	// Some versions of JScript fail to enumerate over properties, names of which
	// correspond to non-enumerable properties in the prototype chain
	var IS_DONTENUM_BUGGY = (function(){
		for (var p in { toString: 1 }) {
			// check actual property name, so that it works with augmented Object.prototype
			if (p === 'toString') return false;
		}
		return true;
	})();

	// Get argument names from method as array
	function getArgumentNames(method) {
		var names = method.toString().match(/^[\s\(]*function[^(]*\(([^)]*)\)/)[1]
			.replace(/\/\/.*?[\r\n]|\/\*(?:.|[\r\n])*?\*\//g, '')
			.replace(/\s+/g, '').split(',');
		return names.length == 1 && !names[0] ? [] : names;
	}

	// Wrap a given method
	function wrap(method, wrapper) {
		return function() {
			var args = $.makeArray(arguments);
			args.unshift($.proxy(method, this));
			return wrapper.apply(this, args);
		}
	}

	// Grab all keys from an object
	// keys({ a: 1, b: 2 }); // => ["a", "b"];
	function keys(object) {
		if (Object.keys) {
			return Object.keys(object);
		} else {
			var results = [];
			for (var property in object) {
				if (object.hasOwnProperty(property)) {
					results.push(property);
				}
			}
			return results;
		}
	}

	function subclass() {};
	function create() {
		var parent = null, properties = $.makeArray(arguments);
		if ($.isFunction(properties[0]))
			parent = properties.shift();

		function klass() {
			this.initialize.apply(this, arguments);
		}

		$.extend(klass, Class.Methods);
		klass.superclass = parent;
		klass.subclasses = [];

		if (parent) {
			subclass.prototype = parent.prototype;
			klass.prototype = new subclass;
			parent.subclasses.push(klass);
		}

		for (var i = 0, length = properties.length; i < length; i++)
			klass.addMethods(properties[i]);

		if (!klass.prototype.initialize)
			klass.prototype.initialize = $.noop;

		klass.prototype.constructor = klass;
		return klass;
	}

	function addMethods(source) {
		var ancestor = this.superclass && this.superclass.prototype,
			properties = keys(source);

		// IE6 doesn't enumerate `toString` and `valueOf` (among other built-in `Object.prototype`) properties,
		// Force copy if they're not Object.prototype ones.
		// Do not copy other Object.prototype.* for performance reasons
		if (IS_DONTENUM_BUGGY) {
			if (source.toString != Object.prototype.toString)
				properties.push("toString");
			if (source.valueOf != Object.prototype.valueOf)
				properties.push("valueOf");
		}

		for (var i = 0, length = properties.length; i < length; i++) {
			var property = properties[i], value = source[property];
			if (ancestor && $.isFunction(value) &&
				getArgumentNames(value)[0] == "$super") {
				var method = value;
				value = wrap(function(m) {
					return function() { return ancestor[m].apply(this, arguments); };
				}(property), method);

				value.valueOf = $.proxy(method.valueOf, method);
				value.toString = $.proxy(method.toString, method);
			}
			this.prototype[property] = value;
		}

		return this;
	}

	return {
		create: create,
		keys: keys,
		Methods: {
			addMethods: addMethods
		}
	};
})(jQuery);