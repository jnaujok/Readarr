# Note (not a roadmap)

This file is a personal parking lot so an idea is not lost. It is **not** a commitment, a schedule, or part of the product roadmap.

Conversion inside Readarr is a non-starter for now. Do not reimplement Calibre’s `ebook-convert` pipeline, and do not take on format-changing as a product feature in this pass.

## Idea: Universal E-Book Format (not a Calibre replacement)

Write a converter **down to an extended markdown format**, which we would eventually write our own book reader for. Also create converters that change that markdown into any of the supported formats.

This is not meant to be a replacement for Calibre. It is the first step towards a **Universal E-Book Format**, so we can ditch all the proprietary versions of ebooks.

Calibre Content Server remains the optional path for people who already want Calibre’s library, device profiles, and conversion engine.
