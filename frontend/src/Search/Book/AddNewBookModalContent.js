import PropTypes from 'prop-types';
import React, { Component } from 'react';
import TextTruncate from 'react-text-truncate';
import BookCover from 'Book/BookCover';
import CheckInput from 'Components/Form/CheckInput';
import FormGroup from 'Components/Form/FormGroup';
import FormInputGroup from 'Components/Form/FormInputGroup';
import FormLabel from 'Components/Form/FormLabel';
import SpinnerButton from 'Components/Link/SpinnerButton';
import ModalBody from 'Components/Modal/ModalBody';
import ModalContent from 'Components/Modal/ModalContent';
import ModalFooter from 'Components/Modal/ModalFooter';
import ModalHeader from 'Components/Modal/ModalHeader';
import { inputTypes, kinds } from 'Helpers/Props';
import stripHtml from 'Utilities/String/stripHtml';
import translate from 'Utilities/String/translate';
import AddAuthorOptionsForm from '../Common/AddAuthorOptionsForm.js';
import styles from './AddNewBookModalContent.css';

class AddNewBookModalContent extends Component {

  //
  // Lifecycle

  constructor(props, context) {
    super(props, context);

    this.state = {
      searchForNewBook: false
    };
  }

  //
  // Listeners

  onSearchForNewBookChange = ({ value }) => {
    this.setState({ searchForNewBook: value });
  };

  onAddBookPress = () => {
    this.props.onAddBookPress(this.state.searchForNewBook);
  };

  //
  // Render

  render() {
    const {
      bookTitle,
      seriesTitle,
      authorName,
      disambiguation,
      overview,
      images,
      isAdding,
      isExistingAuthor,
      isSmallScreen,
      anyEditionOk,
      onModalClose,
      onInputChange,
      ...otherProps
    } = this.props;

    return (
      <ModalContent onModalClose={onModalClose}>
        <ModalHeader>
          {translate('AddNewBook')}
        </ModalHeader>

        <ModalBody>
          <div className={styles.container}>
            {
              isSmallScreen ?
                null:
                <div className={styles.poster}>
                  <BookCover
                    className={styles.poster}
                    images={images}
                    size={250}
                  />
                </div>
            }

            <div className={styles.info}>
              <div className={styles.name}>
                {bookTitle}
              </div>

              {
                !!disambiguation &&
                  <span className={styles.disambiguation}>({disambiguation})</span>
              }

              {
                !!seriesTitle &&
                  <div className={styles.series}>
                    {seriesTitle}
                  </div>
              }

              <div>
                <span className={styles.authorName}> By: {authorName}</span>
              </div>

              {
                overview ?
                  <div className={styles.overview}>
                    <TextTruncate
                      truncateText="…"
                      line={8}
                      text={stripHtml(overview)}
                    />
                  </div> :
                  null
              }

              {
                !isExistingAuthor &&
                  <AddAuthorOptionsForm
                    authorName={authorName}
                    includeNoneMetadataProfile={true}
                    includeSpecificBookMonitor={true}
                    anyEditionOk={anyEditionOk}
                    onInputChange={onInputChange}
                    {...otherProps}
                  />
              }

              {
                isExistingAuthor &&
                  <FormGroup>
                    <FormLabel>
                      {translate('AutomaticallySwitchEdition')}
                    </FormLabel>

                    <FormInputGroup
                      type={inputTypes.CHECK}
                      name="anyEditionOk"
                      helpText={translate('AnyEditionOkHelpText')}
                      onChange={onInputChange}
                      {...anyEditionOk}
                    />
                  </FormGroup>
              }
            </div>
          </div>
        </ModalBody>

        <ModalFooter className={styles.modalFooter}>
          <label className={styles.searchForNewBookLabelContainer}>
            <span className={styles.searchForNewBookLabel}>
              Start search for new book
            </span>

            <CheckInput
              containerClassName={styles.searchForNewBookContainer}
              className={styles.searchForNewBookInput}
              name="searchForNewBook"
              value={this.state.searchForNewBook}
              onChange={this.onSearchForNewBookChange}
            />
          </label>

          <SpinnerButton
            className={styles.addButton}
            kind={kinds.SUCCESS}
            isSpinning={isAdding}
            onPress={this.onAddBookPress}
          >
            Add {bookTitle}
          </SpinnerButton>
        </ModalFooter>
      </ModalContent>
    );
  }
}

AddNewBookModalContent.propTypes = {
  bookTitle: PropTypes.string.isRequired,
  seriesTitle: PropTypes.string,
  authorName: PropTypes.string.isRequired,
  disambiguation: PropTypes.string,
  overview: PropTypes.string,
  images: PropTypes.arrayOf(PropTypes.object).isRequired,
  isAdding: PropTypes.bool.isRequired,
  addError: PropTypes.object,
  isExistingAuthor: PropTypes.bool.isRequired,
  isSmallScreen: PropTypes.bool.isRequired,
  anyEditionOk: PropTypes.object,
  onModalClose: PropTypes.func.isRequired,
  onAddBookPress: PropTypes.func.isRequired,
  onInputChange: PropTypes.func.isRequired
};

AddNewBookModalContent.defaultProps = {
  anyEditionOk: { value: true }
};

export default AddNewBookModalContent;
