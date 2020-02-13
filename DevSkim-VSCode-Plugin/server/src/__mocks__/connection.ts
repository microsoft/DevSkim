import {noop} from "@babel/types";

export class Console {
    log(): void {
        noop();
    }

}
export class Connection {
    public console: Console;
}