// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

import {noop} from "@babel/types";

export class Console {
    log(): void {
        noop();
    }

}
export class Connection {
    public console: Console;
}