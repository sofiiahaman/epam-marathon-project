@group @l1
Feature: Group pre-order participants

  The owner can inspect the participants and their dishes before submitting
  the group's consolidated pre-order.

  Rule: The owner sees the current group orders

    @ui
    Scenario: Owner sees participants, dishes, quantities and totals
      Given the owner has a group pre-order with a guest
      Then the owner sees 2 group participants
      And the owner sees participant "Owner"
      And the owner sees participant "AQA-Sample-Guest"

    @ui
    Scenario: The total changes after a guest changes the basket
      Given the owner has a group pre-order with a guest
      When the guest changes the basket quantity to 3
      When the owner refreshes the group participants
      Then the owner sees participant "AQA-Sample-Guest"

    @ui
    Scenario: The owner sees an unconfirmed participant without dishes
      Given the owner has a group pre-order with a guest without dishes
      Then the owner sees 2 group participants
      And the owner sees participant "AQA-Sample-Guest"

    @ui
    Scenario: The owner sees an empty group
      Given the owner has an empty group pre-order
      Then the owner sees 0 group participants

    @api
    Scenario Outline: Only the group owner can read participants
      When the <caller> requests the group participants
      Then the group request is refused with status <status>

      Examples:
        | caller          | status |
        | another client  | 403    |
        | a guest         | 403    |
        | a waiter        | 403    |
        | an anonymous user | 401  |

    @api
    Scenario: A missing group returns not found
      When an authorized owner requests a missing group
      Then the group request is refused with status 404
