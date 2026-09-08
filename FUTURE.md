# Future

Conversion inside Readarr is a **non-starter** for now. We will not reimplement Calibre’s `ebook-convert` pipeline, and we will not take on format-changing as a product feature in this pass.

This note saves a later idea so it is not lost.

## Universal E-Book Format (not a Calibre replacement)

We will write a converter **down to an extended markdown format**, which we will eventually write our own book reader for. We will also create converters that change that markdown into any of the supported formats.

This is not meant to be a replacement for Calibre. It is the first step towards a **Universal E-Book Format**, so we can ditch all the proprietary versions of ebooks.

Calibre Content Server remains the optional path for people who already want Calibre’s library, device profiles, and conversion engine.
