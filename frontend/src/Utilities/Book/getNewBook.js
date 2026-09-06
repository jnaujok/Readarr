import getNewAuthor from 'Utilities/Author/getNewAuthor';

function getNewBook(book, payload) {
  const {
    searchForNewBook = false,
    anyEditionOk = true
  } = payload;

  if (!('id' in book.author) || book.author.id === 0) {
    getNewAuthor(book.author, payload);

    if (payload.monitor === 'specificBook') {
      delete book.author.addOptions.monitor;
      book.author.addOptions.booksToMonitor = [book.foreignBookId];
    }
  }

  book.addOptions = {
    searchForNewBook
  };
  book.monitored = true;
  book.anyEditionOk = anyEditionOk;

  return book;
}

export default getNewBook;
