
function getNewAuthor(author, payload) {
  const {
    rootFolderPath,
    monitor,
    monitorNewItems,
    qualityProfileId,
    metadataProfileId,
    tags,
    searchForMissingBooks = false,
    anyEditionOk = true
  } = payload;

  const addOptions = {
    monitor,
    searchForMissingBooks,
    anyEditionOk
  };

  author.addOptions = addOptions;
  author.monitored = true;
  author.monitorNewItems = monitorNewItems;
  author.qualityProfileId = qualityProfileId;
  author.metadataProfileId = metadataProfileId;
  author.rootFolderPath = rootFolderPath;
  author.tags = tags;

  return author;
}

export default getNewAuthor;
