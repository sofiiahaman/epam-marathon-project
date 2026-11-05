@group @l3
Feature: Remove a group pre-order participant

  The owner can remove guest sessions before submitting the remaining group order.

  Rule: Removing a participant updates the group snapshot

    @api
    Scenario: Removing one of several guests keeps the other guest and recalculates the total
      Given the owner has several removable guests
      When the owner deletes the first guest
      Then the delete response has status 200
      And the remaining participant count is 2
      And the participant total is recalculated

    @api
    Scenario: The owner can remove a guest who already submitted
      Given the owner has several submitted removable guests
      When the owner deletes the first submitted guest
      Then the delete response has status 200
      And the remaining participant count is 2

    @api
    Scenario: Removing the last guest returns an empty participant list
      Given the owner has one removable guest
      When the owner deletes the only guest
      Then the delete response has status 200
      And the remaining participant count is 1
      And the participant total is recalculated

    @api
    Scenario: A participant from another group cannot be removed
      Given the owner has two independent groups
      When the owner deletes the first group's guest from the second group
      Then the delete response has status 400

    @api
    Scenario: A missing participant returns not found
      Given the owner has one removable guest
      When the owner deletes a missing guest
      Then the delete response has status 404

    @api
    Scenario: A non-owner cannot remove a participant
      Given the owner has one removable guest
      When another customer deletes the guest session
      Then the delete response has status 403

  Rule: The submitted order excludes a removed participant

    @ui     @ui
    Scenario: Owner reviews, removes a guest, submits, and waiter sees the remaining order
      Given the owner has several submitted removable guests
      And the owner has opened the group pre-order
      When the owner deletes the first submitted guest
      And the owner submits the remaining group order
      Then the remaining order excludes the deleted guest
